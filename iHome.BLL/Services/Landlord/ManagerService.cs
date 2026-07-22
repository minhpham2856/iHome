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
			// User/manager data access against shared DbContext
			_users = new UserRepository(context);
			// Building assignment sync
			_buildings = new BuildingRepository(context);
			// Property ownership checks
			_properties = new PropertyRepository(context);
			// Audit trail for create/update/delete operations
			_audits = new AuditLogRepository(context);
		}

		// Grid of managers across landlord properties
		public List<ManagerDto> GetByLandlord(int landlordId)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Load managers for landlord and map to list DTOs
			return _users.GetManagersForLandlord(landlordId)
				.Select(m => Map(m, landlordId))
				.ToList();
		}

		// Edit form — load BuildingIds currently assigned to manager at active property
		public ManagerFormDto? GetForm(int landlordId, int managerId)
		{
			// Verify manager belongs to landlord's property scope
			var manager = RequireManagedManager(landlordId, managerId);
			// Manager must have a managed property assigned
			int propertyId = manager.ManagedPropertyId
				?? throw new InvalidOperationException("Nhân viên chưa được gán nhà trọ.");

			// Load building ids assigned to this manager at the property
			var assigned = _buildings.GetByManager(managerId)
				.Where(b => b.PropertyId == propertyId)
				.Select(b => b.Id)
				.ToList();

			// Project manager fields and building selection into form DTO
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
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Load active landlord properties and map to combo DTOs
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
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Verify property ownership
			RequireOwnedProperty(landlordId, propertyId);
			// Materialize selected building ids for lookup
			var selected = selectedBuildingIds?.ToHashSet() ?? new HashSet<int>();
			// Load active buildings and compute assignment flags
			return _buildings.GetByProperty(propertyId)
				.Where(b => b.IsActive)
				.OrderBy(b => b.Name)
				.Select(b =>
				{
					// Building is taken when assigned to a different manager
					bool assignedToOther =
						b.ManagerId.HasValue &&
						(!currentManagerId.HasValue || b.ManagerId.Value != currentManagerId.Value);
					// Build checkbox option with assignment metadata
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
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Validate form fields
			ValidateForm(form);
			// Verify property ownership and load property
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			// Ensure all building ids belong to selected property
			ValidateBuildingsForProperty(form.PropertyId, form.BuildingIds);
			// Ensure buildings are not assigned to another manager
			EnsureBuildingsAvailable(form.BuildingIds, excludeManagerId: null);

			// Delegate account creation to AuthService
			var created = _auth.CreateManagerAccount(new User
			{
				FullName = form.FullName,
				Email = form.Email,
				PhoneNumber = form.PhoneNumber,
				ManagedPropertyId = property.Id
			});

			// Optionally sync building assignments after UserId exists
			if (form.BuildingIds != null && form.BuildingIds.Count > 0)
			{
				_buildings.SyncManagerAssignments(property.Id, created.UserId, form.BuildingIds);
			}

			// Record create audit with new-value snapshot
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
			// Return credentials DTO for UI display/email
			return created;
		}

		// Update manager profile and property/building assignments
		public void Update(int landlordId, ManagerFormDto form)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Validate form fields
			ValidateForm(form);
			// Load manager and verify landlord scope
			var manager = RequireManagedManager(landlordId, form.Id);
			// Capture previous property id for assignment cleanup
			int oldPropertyId = manager.ManagedPropertyId
				?? throw new InvalidOperationException("Nhân viên chưa được gán nhà trọ.");
			// Verify new property ownership
			var newProperty = RequireOwnedProperty(landlordId, form.PropertyId);
			// Ensure all building ids belong to selected property
			ValidateBuildingsForProperty(form.PropertyId, form.BuildingIds);
			// Ensure buildings are not assigned to another manager (exclude self)
			EnsureBuildingsAvailable(form.BuildingIds, excludeManagerId: form.Id);
			// Guard unique email across users
			_auth.EnsureUniqueEmail(form.Email.Trim(), form.Id);

			// Snapshot values before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.FullName] = manager.FullName,
				[AuditField.Email] = manager.Email,
				[AuditField.Active] = DisplayText.FormatActive(manager.IsActive),
				[AuditField.PropertyId] = oldPropertyId.ToString()
			};

			// Apply form values to tracked manager entity
			manager.FullName = InputFormatter.FormatFullName(form.FullName);
			manager.Email = form.Email.Trim();
			manager.PhoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim();
			manager.IsActive = form.IsActive;
			manager.ManagedPropertyId = newProperty.Id;
			// UPDATE; throw if repository reports failure
			if (!_users.Update(manager))
			{
				throw new InvalidOperationException("Không thể cập nhật nhân viên.");
			}

			// Property change — clear building assignments on old property first
			if (oldPropertyId != newProperty.Id)
			{
				_buildings.SyncManagerAssignments(oldPropertyId, form.Id, Array.Empty<int>());
			}
			// Sync building assignments on new property
			_buildings.SyncManagerAssignments(newProperty.Id, form.Id, form.BuildingIds);

			// Snapshot values after edit for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.FullName] = manager.FullName,
				[AuditField.Email] = manager.Email,
				[AuditField.Active] = DisplayText.FormatActive(manager.IsActive),
				[AuditField.PropertyId] = newProperty.Id.ToString()
			};
			// Build compact old/new diff strings
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			// Write update audit entry
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
			// Load manager and verify landlord scope
			var manager = RequireManagedManager(landlordId, managerId);
			// Block delete when manager created contracts or received payments
			if (_users.HasContractOrPaymentReferences(manager))
			{
				throw new InvalidOperationException(
					"Không thể xóa vì nhân viên đã tạo hợp đồng hoặc nhận thanh toán. Hãy bỏ tích Đang hoạt động khi sửa.");
			}

			// Build delete audit snapshot before row removal
			string? snapshot = AuditDiff.FormatOldOnly(new Dictionary<string, string?>
			{
				[AuditField.Username] = manager.Username,
				[AuditField.FullName] = manager.FullName,
				[AuditField.Email] = manager.Email
			});
			// Clear building manager references
			_buildings.ClearManagerAssignments(manager);
			// Remove manager-related audit rows owned by user
			_users.DeleteAuditLogs(manager);
			// DELETE; throw if repository reports failure
			if (!_users.Delete(manager))
			{
				throw new InvalidOperationException("Không thể xóa nhân viên.");
			}

			// Record delete audit entry
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
			// Load user by primary key
			var user = _users.GetById(landlordId);
			// Reject missing user or non-landlord role
			if (user == null || user.Role != UserRole.Landlord)
			{
				throw new UnauthorizedAccessException("Chỉ chủ nhà mới quản lý nhân viên.");
			}
		}

		// Property must be active and owned by landlord
		private Property RequireOwnedProperty(int landlordId, int propertyId)
		{
			// Load property by primary key
			var property = _properties.GetById(propertyId);
			// Reject missing, foreign-owned, or inactive properties
			if (property == null || property.LandlordId != landlordId || !property.IsActive)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn hoặc đã ngừng.");
			}
			// Return verified entity to caller
			return property;
		}

		// Manager must belong to one of this landlord's properties
		private User RequireManagedManager(int landlordId, int managerId)
		{
			// Load manager user by primary key
			var manager = _users.GetById(managerId);
			// Reject missing user or wrong role
			if (manager == null || manager.Role != UserRole.Manager)
			{
				throw new UnauthorizedAccessException("Nhân viên không hợp lệ.");
			}

			// Reject manager without property assignment
			if (!manager.ManagedPropertyId.HasValue)
			{
				throw new UnauthorizedAccessException("Nhân viên không thuộc hệ thống nhà trọ của bạn.");
			}

			// Verify managed property belongs to this landlord
			var property = _properties.GetById(manager.ManagedPropertyId.Value);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhân viên không thuộc hệ thống nhà trọ của bạn.");
			}
			// Return verified manager entity
			return manager;
		}

		// Every buildingId must belong to propertyId
		private void ValidateBuildingsForProperty(int propertyId, IReadOnlyCollection<int>? buildingIds)
		{
			// Empty selection is valid
			if (buildingIds == null || buildingIds.Count == 0) return;
			// Build set of building ids owned by property
			var owned = _buildings.GetByProperty(propertyId).Select(b => b.Id).ToHashSet();
			// Reject any building outside property scope
			if (buildingIds.Any(id => !owned.Contains(id)))
			{
				throw new InvalidOperationException("Có tòa nhà không thuộc nhà trọ đã chọn.");
			}
		}

		// Each building may have only one manager — block stealing from another manager
		private void EnsureBuildingsAvailable(IReadOnlyCollection<int>? buildingIds, int? excludeManagerId)
		{
			// Empty selection is valid
			if (buildingIds == null || buildingIds.Count == 0) return;
			// Collect human-readable conflict messages
			var conflicts = new List<string>();
			// Check each requested building for manager conflicts
			foreach (int buildingId in buildingIds)
			{
				// Load building by primary key
				var building = _buildings.GetById(buildingId);
				// Skip missing building rows
				if (building == null) continue;
				// Flag when assigned to a different manager
				if (building.ManagerId.HasValue &&
					(!excludeManagerId.HasValue || building.ManagerId.Value != excludeManagerId.Value))
				{
					string who = building.Manager?.FullName ?? $"#{building.ManagerId}";
					conflicts.Add($"{building.Name} (đang gán: {who})");
				}
			}
			// Throw aggregated conflict message when any building is taken
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
			// Reject empty full name
			if (string.IsNullOrWhiteSpace(form.FullName))
			{
				throw new ArgumentException("Họ tên không được để trống.");
			}
			// Reject invalid email
			if (string.IsNullOrWhiteSpace(form.Email) || !form.Email.Contains('@'))
			{
				throw new ArgumentException("Email không hợp lệ.");
			}
			// Reject missing property selection
			if (form.PropertyId <= 0)
			{
				throw new ArgumentException("Chọn nhà trọ để phân công.");
			}
		}

		// Map User entity to manager list DTO with assignment summary
		private ManagerDto Map(User m, int landlordId)
		{
			// Resolve managed property from navigation or lookup
			var property = m.ManagedProperty
				?? (m.ManagedPropertyId.HasValue ? _properties.GetById(m.ManagedPropertyId.Value) : null);

			// Load buildings assigned to manager within landlord scope
			var assigned = _buildings.GetByManager(m.Id)
				.Where(b => b.Property.LandlordId == landlordId)
				.OrderBy(b => b.Name)
				.ToList();

			// Project manager fields and assignment summary into list DTO
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
