using iHome.BLL.Common;
using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services
{
	// Landlord manages staff (Manager): CRUD, assign property + buildings, delegate account creation to AuthService
	public class LandlordManagerService
	{
		private readonly UserRepository _users;
		private readonly BuildingRepository _buildings;
		private readonly PropertyRepository _properties;
		private readonly AuditLogRepository _audits;
		private readonly AuthService _auth = new();

		public LandlordManagerService() : this(new IHomeDbContext()) { }

		public LandlordManagerService(IHomeDbContext context)
		{
			_users = new UserRepository(context);
			_buildings = new BuildingRepository(context);
			_properties = new PropertyRepository(context);
			_audits = new AuditLogRepository(context);
		}

		// Grid of managers across landlord properties
		public List<ManagerDto> GetByLandlord(int landlordId)
		{
			EnsureLandlord(landlordId);
			return _users.GetManagersForLandlord(landlordId)
				.Select(m => Map(m, landlordId))
				.ToList();
		}

		// Edit form — load BuildingIds currently assigned to manager at active property
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

		// Active property combo for assigning new/edited manager
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

		// Building checkbox list — mark buildings assigned to another manager (IsAssignedToOther)
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

		// Create manager via AuthService plus optional building assignment sync
		public ManagerAccountCreatedDto Create(int landlordId, ManagerFormDto form)
		{
			EnsureLandlord(landlordId);
			ValidateForm(form);
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			ValidateBuildingsForProperty(form.PropertyId, form.BuildingIds);
			EnsureBuildingsAvailable(form.BuildingIds, excludeManagerId: null);

			var created = _auth.CreateManagerAccount(new User
			{
				FullName = form.FullName,
				Email = form.Email,
				PhoneNumber = form.PhoneNumber,
				ManagedPropertyId = property.Id
			});

			if (form.BuildingIds != null && form.BuildingIds.Count > 0)
			{
				_buildings.SyncManagerAssignments(property.Id, created.UserId, form.BuildingIds);
			}

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Create,
				TableName = AuditObject.Users,
				RecordId = created.UserId.ToString(),
				Detail = created.Username,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.Username] = created.Username,
					[AuditField.PropertyId] = property.Id.ToString(),
					[AuditField.Active] = DisplayText.FormatActive(true)
				}),
				Timestamp = DateTime.Now
			});
			return created;
		}

		// Update manager profile and property/building assignments
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

			var before = new Dictionary<string, string?>
			{
				[AuditField.FullName] = manager.FullName,
				[AuditField.Email] = manager.Email,
				[AuditField.Active] = DisplayText.FormatActive(manager.IsActive),
				[AuditField.PropertyId] = oldPropertyId.ToString()
			};

			manager.FullName = InputFormatter.FormatFullName(form.FullName);
			manager.Email = form.Email.Trim();
			manager.PhoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim();
			manager.IsActive = form.IsActive;
			manager.ManagedPropertyId = newProperty.Id;
			if (!_users.Update(manager))
			{
				throw new InvalidOperationException("Không thể cập nhật nhân viên.");
			}

			if (oldPropertyId != newProperty.Id)
			{
				_buildings.SyncManagerAssignments(oldPropertyId, form.Id, Array.Empty<int>());
			}
			_buildings.SyncManagerAssignments(newProperty.Id, form.Id, form.BuildingIds);

			var after = new Dictionary<string, string?>
			{
				[AuditField.FullName] = manager.FullName,
				[AuditField.Email] = manager.Email,
				[AuditField.Active] = DisplayText.FormatActive(manager.IsActive),
				[AuditField.PropertyId] = newProperty.Id.ToString()
			};
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Update,
				TableName = AuditObject.Users,
				RecordId = form.Id.ToString(),
				Detail = manager.Username,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Hard delete manager — block when contract/payment references exist
		public void Delete(int landlordId, int managerId)
		{
			var manager = RequireManagedManager(landlordId, managerId);
			if (_users.HasContractOrPaymentReferences(manager))
			{
				throw new InvalidOperationException(
					"Không thể xóa vì nhân viên đã tạo hợp đồng hoặc nhận thanh toán. Hãy bỏ tích Đang hoạt động khi sửa.");
			}

			string? snapshot = AuditDiff.FormatOldOnly(new Dictionary<string, string?>
			{
				[AuditField.Username] = manager.Username,
				[AuditField.FullName] = manager.FullName,
				[AuditField.Email] = manager.Email
			});
			_buildings.ClearManagerAssignments(manager);
			_users.DeleteAuditLogs(manager);
			if (!_users.Delete(manager))
			{
				throw new InvalidOperationException("Không thể xóa nhân viên.");
			}

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Delete,
				TableName = AuditObject.Users,
				RecordId = managerId.ToString(),
				Detail = manager.Username,
				OldValue = snapshot,
				NewValue = null,
				Timestamp = DateTime.Now
			});
		}

		// Guard caller has Landlord role
		private void EnsureLandlord(int landlordId)
		{
			var user = _users.GetById(landlordId);
			if (user == null || user.Role != UserRole.Landlord)
			{
				throw new UnauthorizedAccessException("Chỉ chủ nhà mới quản lý nhân viên.");
			}
		}

		// Property must be active and owned by landlord
		private Property RequireOwnedProperty(int landlordId, int propertyId)
		{
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId || !property.IsActive)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn hoặc đã ngừng.");
			}
			return property;
		}

		// Manager must belong to one of this landlord's properties
		private User RequireManagedManager(int landlordId, int managerId)
		{
			var manager = _users.GetById(managerId);
			if (manager == null || manager.Role != UserRole.Manager)
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

		// Every buildingId must belong to propertyId
		private void ValidateBuildingsForProperty(int propertyId, IReadOnlyCollection<int>? buildingIds)
		{
			if (buildingIds == null || buildingIds.Count == 0) return;
			var owned = _buildings.GetByProperty(propertyId).Select(b => b.Id).ToHashSet();
			if (buildingIds.Any(id => !owned.Contains(id)))
			{
				throw new InvalidOperationException("Có tòa nhà không thuộc nhà trọ đã chọn.");
			}
		}

		// Each building may have only one manager — block stealing from another manager
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

		// Validate manager form fields
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

		// Map User entity to manager list DTO with assignment summary
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
