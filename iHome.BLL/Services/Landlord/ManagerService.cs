using iHome.BLL.Common;
using iHome.BLL.DTOs.Landlord;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services
{
	public class LandlordManagerService
	{
		private readonly UserRepository _users;
		private readonly BuildingRepository _buildings;
		private readonly PropertyRepository _properties;
		private readonly AuditLogRepository _audits;
		private readonly AuthService _auth = new();

		public LandlordManagerService() : this(new IHomeDbContext())
		{
		}

		public LandlordManagerService(IHomeDbContext context)
		{
			_users = new UserRepository(context);
			_buildings = new BuildingRepository(context);
			_properties = new PropertyRepository(context);
			_audits = new AuditLogRepository(context);
		}

		public List<ManagerDto> GetByLandlord(int landlordId)
		{
			EnsureLandlord(landlordId);
			return _users.GetManagersForLandlord(landlordId)
				.Select(m => Map(m, landlordId))
				.ToList();
		}

		public ManagerFormDto? GetForm(int landlordId, int managerId)
		{
			var manager = RequireManagedManager(landlordId, managerId);
			int propertyId = manager.ManagedPropertyId
				?? throw new InvalidOperationException("Nhân viên chưa được gán nhà trọ.");

			var assigned = _buildings.GetByManager(managerId)
				.Where(b => b.PropertyId == propertyId)
				.Select(b => b.Id)
				.ToList();

			return new ManagerFormDto
			{
				Id = manager.Id,
				FullName = manager.FullName,
				Email = manager.Email,
				PhoneNumber = manager.PhoneNumber,
				IsActive = manager.IsActive,
				PropertyId = propertyId,
				BuildingIds = assigned
			};
		}

		public List<ManagerPropertyOptionDto> GetPropertyOptions(int landlordId)
		{
			EnsureLandlord(landlordId);
			return _properties.GetByLandlord(landlordId)
				.Where(p => p.IsActive)
				.OrderBy(p => p.Name)
				.Select(p => new ManagerPropertyOptionDto
				{
					Id = p.Id,
					Name = p.Name
				})
				.ToList();
		}

		public List<ManagerBuildingOptionDto> GetBuildingOptions(
			int landlordId,
			int propertyId,
			IEnumerable<int>? selectedBuildingIds = null,
			int? currentManagerId = null)
		{
			EnsureLandlord(landlordId);
			RequireOwnedProperty(landlordId, propertyId);
			var selected = selectedBuildingIds?.ToHashSet() ?? new HashSet<int>();
			return _buildings.GetByProperty(propertyId)
				.Where(b => b.IsActive)
				.OrderBy(b => b.Name)
				.Select(b =>
				{
					bool assignedToOther =
						b.ManagerId.HasValue &&
						(!currentManagerId.HasValue || b.ManagerId.Value != currentManagerId.Value);
					return new ManagerBuildingOptionDto
					{
						BuildingId = b.Id,
						PropertyId = b.PropertyId,
						PropertyName = b.Property?.Name ?? string.Empty,
						BuildingName = b.Name,
						CurrentManagerId = b.ManagerId,
						CurrentManagerName = b.Manager?.FullName,
						IsAssignedToOther = assignedToOther,
						IsSelected = !assignedToOther && selected.Contains(b.Id)
					};
				})
				.ToList();
		}

		public ManagerAccountCreatedDto Create(int landlordId, ManagerFormDto form)
		{
			EnsureLandlord(landlordId);
			ValidateForm(form);
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			ValidateBuildingsForProperty(form.PropertyId, form.BuildingIds);
			EnsureBuildingsAvailable(form.BuildingIds, excludeManagerId: null);

			var created = _auth.CreateManagerAccount(
				form.FullName,
				form.Email,
				form.PhoneNumber,
				property.Id);

			// gán tòa tùy chọn — có thể tạo tài khoản rồi phân công sau
			if (form.BuildingIds != null && form.BuildingIds.Count > 0)
			{
				_buildings.SyncManagerAssignments(property.Id, created.UserId, form.BuildingIds);
			}

			_audits.Add(
				landlordId,
				"Create",
				"Users",
				created.UserId.ToString(),
				null,
				$"Manager={created.Username}; PropertyId={property.Id}; Active=true");
			return created;
		}

		public void Update(int landlordId, ManagerFormDto form)
		{
			EnsureLandlord(landlordId);
			ValidateForm(form);
			var manager = RequireManagedManager(landlordId, form.Id);
			int oldPropertyId = manager.ManagedPropertyId
				?? throw new InvalidOperationException("Nhân viên chưa được gán nhà trọ.");
			var newProperty = RequireOwnedProperty(landlordId, form.PropertyId);
			ValidateBuildingsForProperty(form.PropertyId, form.BuildingIds);
			EnsureBuildingsAvailable(form.BuildingIds, excludeManagerId: form.Id);
			_auth.EnsureUniqueEmail(form.Email.Trim(), form.Id);

			string oldValue =
				$"Name={manager.FullName}; Email={manager.Email}; Active={manager.IsActive}; PropertyId={oldPropertyId}";

			manager.FullName = InputFormatter.FormatFullName(form.FullName);
			manager.Email = form.Email.Trim();
			manager.PhoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim();
			manager.IsActive = form.IsActive;
			manager.ManagedPropertyId = newProperty.Id;
			if (!_users.Update(manager))
			{
				throw new InvalidOperationException("Không thể cập nhật nhân viên.");
			}

			// đổi nhà trọ → bỏ gán tòa nhà cũ trước
			if (oldPropertyId != newProperty.Id)
			{
				_buildings.SyncManagerAssignments(oldPropertyId, form.Id, Array.Empty<int>());
			}
			_buildings.SyncManagerAssignments(newProperty.Id, form.Id, form.BuildingIds);

			_audits.Add(
				landlordId,
				"Update",
				"Users",
				form.Id.ToString(),
				oldValue,
				$"Name={manager.FullName}; Email={manager.Email}; Active={manager.IsActive}; PropertyId={newProperty.Id}");
		}

		public void Delete(int landlordId, int managerId)
		{
			var manager = RequireManagedManager(landlordId, managerId);
			if (_users.HasContractOrPaymentReferences(managerId))
			{
				throw new InvalidOperationException(
					"Không thể xóa vì nhân viên đã tạo hợp đồng hoặc nhận thanh toán. Hãy bỏ tích Đang hoạt động khi sửa.");
			}

			string snapshot = $"Username={manager.Username}; Name={manager.FullName}; Email={manager.Email}";
			_buildings.ClearManagerAssignments(managerId);
			_users.DeleteAuditLogs(managerId);
			if (!_users.Delete(managerId))
			{
				throw new InvalidOperationException("Không thể xóa nhân viên.");
			}

			_audits.Add(landlordId, "Delete", "Users", managerId.ToString(), snapshot, null);
		}

		private void EnsureLandlord(int landlordId)
		{
			var user = _users.GetById(landlordId);
			if (user == null || user.Role != "Landlord")
			{
				throw new UnauthorizedAccessException("Chỉ chủ nhà mới quản lý nhân viên.");
			}
		}

		private Property RequireOwnedProperty(int landlordId, int propertyId)
		{
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId || !property.IsActive)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn hoặc đã ngừng.");
			}
			return property;
		}

		private User RequireManagedManager(int landlordId, int managerId)
		{
			var manager = _users.GetById(managerId);
			if (manager == null || manager.Role != "Manager")
			{
				throw new UnauthorizedAccessException("Nhân viên không hợp lệ.");
			}

			if (!manager.ManagedPropertyId.HasValue)
			{
				throw new UnauthorizedAccessException("Nhân viên không thuộc hệ thống nhà trọ của bạn.");
			}

			var property = _properties.GetById(manager.ManagedPropertyId.Value);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhân viên không thuộc hệ thống nhà trọ của bạn.");
			}
			return manager;
		}

		private void ValidateBuildingsForProperty(int propertyId, IReadOnlyCollection<int>? buildingIds)
		{
			if (buildingIds == null || buildingIds.Count == 0) return;
			var owned = _buildings.GetByProperty(propertyId).Select(b => b.Id).ToHashSet();
			if (buildingIds.Any(id => !owned.Contains(id)))
			{
				throw new InvalidOperationException("Có tòa nhà không thuộc nhà trọ đã chọn.");
			}
		}

		// mỗi tòa chỉ 1 manager — không cho cướp tòa đang thuộc nhân viên khác
		private void EnsureBuildingsAvailable(IReadOnlyCollection<int>? buildingIds, int? excludeManagerId)
		{
			if (buildingIds == null || buildingIds.Count == 0) return;
			var conflicts = new List<string>();
			foreach (int buildingId in buildingIds)
			{
				var building = _buildings.GetById(buildingId);
				if (building == null) continue;
				if (building.ManagerId.HasValue &&
					(!excludeManagerId.HasValue || building.ManagerId.Value != excludeManagerId.Value))
				{
					string who = building.Manager?.FullName ?? $"#{building.ManagerId}";
					conflicts.Add($"{building.Name} (đang gán: {who})");
				}
			}
			if (conflicts.Count > 0)
			{
				throw new InvalidOperationException(
					"Không thể gán các tòa đang thuộc nhân viên khác. Hãy bỏ gán ở nhân viên đó trước:\n" +
					string.Join("\n", conflicts));
			}
		}

		private static void ValidateForm(ManagerFormDto form)
		{
			if (string.IsNullOrWhiteSpace(form.FullName))
			{
				throw new ArgumentException("Họ tên không được để trống.");
			}
			if (string.IsNullOrWhiteSpace(form.Email) || !form.Email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			if (form.PropertyId <= 0)
			{
				throw new ArgumentException("Chọn nhà trọ để phân công.");
			}
		}

		private ManagerDto Map(User m, int landlordId)
		{
			var property = m.ManagedProperty
				?? (m.ManagedPropertyId.HasValue ? _properties.GetById(m.ManagedPropertyId.Value) : null);

			var assigned = _buildings.GetByManager(m.Id)
				.Where(b => b.Property.LandlordId == landlordId)
				.OrderBy(b => b.Name)
				.ToList();

			return new ManagerDto
			{
				Id = m.Id,
				FullName = m.FullName,
				Username = m.Username,
				Email = m.Email,
				PhoneNumber = m.PhoneNumber,
				IsActive = m.IsActive,
				StatusDisplay = m.IsActive ? "Đang hoạt động" : "Ngừng hoạt động",
				PropertyId = property?.Id ?? 0,
				PropertyName = property?.Name ?? "-",
				AssignedBuildingCount = assigned.Count,
				AssignedBuildingsSummary = assigned.Count == 0
					? "Chưa phân công tòa"
					: string.Join(", ", assigned.Select(b => b.Name))
			};
		}
	}
}
