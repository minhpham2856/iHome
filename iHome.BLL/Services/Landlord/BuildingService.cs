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
	// CRUD buildings (Building) within a landlord property — optional Manager assignment, audit every change
	public class LandlordBuildingService
	{
		private readonly BuildingRepository _buildings;
		private readonly PropertyRepository _properties;
		private readonly UserRepository _users;
		private readonly AuditLogRepository _audits;

		public LandlordBuildingService() : this(new IHomeDbContext()) { }

		public LandlordBuildingService(IHomeDbContext context)
		{
			// Building data access against shared DbContext
			_buildings = new BuildingRepository(context);
			// Property ownership checks
			_properties = new PropertyRepository(context);
			// Manager lookup for assignment validation
			_users = new UserRepository(context);
			// Audit trail for create/update operations
			_audits = new AuditLogRepository(context);
		}

		// List buildings by property — verify property belongs to landlord first
		public List<BuildingDto> GetByProperty(int landlordId, int propertyId)
		{
			// Guard property ownership before querying buildings
			RequireOwnedProperty(landlordId, propertyId);
			// Load buildings for property and map to list DTOs
			return _buildings.GetByProperty(propertyId).Select(Map).ToList();
		}

		// Edit form — load ManagerId, IsActive, and floor count
		public BuildingFormDto? GetForm(int landlordId, int buildingId)
		{
			// Verify building belongs to landlord via property chain
			var building = RequireOwnedBuilding(landlordId, buildingId);
			// Project entity fields into form DTO for two-way binding
			return new BuildingFormDto
			{
				Id = building.Id,
				PropertyId = building.PropertyId,
				Name = building.Name,
				NumberOfFloors = building.NumberOfFloors,
				Description = building.Description,
				ManagerId = building.ManagerId,
				IsActive = building.IsActive
			};
		}

		// Manager combo for building assignment — only managers assigned to propertyId
		public List<ManagerOptionDto> GetManagerOptions(int propertyId)
		{
			// Start with unassigned placeholder option
			var options = new List<ManagerOptionDto>
			{
				new() { Id = null, Name = "Chưa gán" }
			};

			// Append active managers scoped to this property
			options.AddRange(_users.GetManagersForProperty(propertyId).Select(m => new ManagerOptionDto
			{
				Id = m.Id,
				Name = m.FullName
			}));
			// Return combo options for UI binding
			return options;
		}

		// Create new building — IsActive forced false when parent property is inactive
		public void Create(int landlordId, BuildingFormDto form)
		{
			// Validate required form fields
			Validate(form);
			// Verify property ownership and load parent property
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			// Ensure selected manager is valid for this property
			ValidateManager(form.ManagerId, form.PropertyId);

			// Inactive property cannot produce an active building
			bool isActive = property.IsActive && form.IsActive;
			// Build new Building entity from trimmed form values
			var entity = new Building
			{
				PropertyId = form.PropertyId,
				Name = form.Name.Trim(),
				NumberOfFloors = form.NumberOfFloors,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
				ManagerId = form.ManagerId,
				IsActive = isActive
			};
			// INSERT; throw if repository reports failure
			if (!_buildings.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm tòa nhà.");
			}

			// Record create audit with new-value snapshot
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Create,
				TableName = AuditObject.Buildings,
				RecordId = entity.Id.ToString(),
				Detail = entity.Name,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.Name] = entity.Name,
					[AuditField.PropertyId] = entity.PropertyId.ToString(),
					[AuditField.ManagerId] = entity.ManagerId?.ToString(),
					[AuditField.Active] = DisplayText.FormatActive(entity.IsActive)
				}),
				Timestamp = DateTime.Now
			});
		}

		// Update building — diff Name/Floors/Manager/Active
		public void Update(int landlordId, BuildingFormDto form)
		{
			// Validate required form fields
			Validate(form);
			// Load existing building and verify ownership
			var existing = RequireOwnedBuilding(landlordId, form.Id);
			// Load parent property for active-state cascade rule
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			// Ensure selected manager is valid for this property
			ValidateManager(form.ManagerId, form.PropertyId);

			// Inactive property cannot produce an active building
			bool isActive = property.IsActive && form.IsActive;
			// Normalize building name
			string name = form.Name.Trim();
			// Snapshot values before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.Name] = existing.Name,
				[AuditField.Floors] = existing.NumberOfFloors.ToString(),
				[AuditField.ManagerId] = existing.ManagerId?.ToString(),
				[AuditField.Active] = DisplayText.FormatActive(existing.IsActive)
			};
			// Snapshot values after edit for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.Name] = name,
				[AuditField.Floors] = form.NumberOfFloors.ToString(),
				[AuditField.ManagerId] = form.ManagerId?.ToString(),
				[AuditField.Active] = DisplayText.FormatActive(isActive)
			};
			// Build compact old/new diff strings
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			// Persist updated building row
			_buildings.Update(new Building
			{
				Id = form.Id,
				Name = name,
				NumberOfFloors = form.NumberOfFloors,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
				ManagerId = form.ManagerId,
				IsActive = isActive
			});

			// Write update audit entry
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Update,
				TableName = AuditObject.Buildings,
				RecordId = form.Id.ToString(),
				Detail = name,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Guard property belongs to landlord
		private Property RequireOwnedProperty(int landlordId, int propertyId)
		{
			// Load property by primary key
			var property = _properties.GetById(propertyId);
			// Reject missing or foreign-owned properties
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
			// Return verified entity to caller
			return property;
		}

		// Guard building → property → landlord ownership chain
		private Building RequireOwnedBuilding(int landlordId, int buildingId)
		{
			// Load building with navigation to property
			var building = _buildings.GetById(buildingId);
			// Reject missing building or property not owned by landlord
			if (building == null || building.Property == null || building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Tòa nhà không thuộc về bạn.");
			}
			// Return verified entity to caller
			return building;
		}

		// Manager must have Role=Manager, be active, and ManagedPropertyId = propertyId
		private void ValidateManager(int? managerId, int propertyId)
		{
			// Null manager is allowed (unassigned building)
			if (!managerId.HasValue) return;
			// Load selected manager user record
			var manager = _users.GetById(managerId.Value);
			// Reject invalid role or inactive manager
			if (manager == null || manager.Role != UserRole.Manager || !manager.IsActive)
			{
				throw new ArgumentException("Quản lý được chọn không hợp lệ.");
			}
			// Reject manager assigned to a different property
			if (manager.ManagedPropertyId != propertyId)
			{
				throw new ArgumentException("Nhân viên này được gán nhà trọ khác.");
			}
		}

		// Basic form validation
		private static void Validate(BuildingFormDto form)
		{
			// Reject empty building name
			if (string.IsNullOrWhiteSpace(form.Name))
			{
				throw new ArgumentException("Tên tòa nhà không được để trống.");
			}
			// Reject invalid floor count
			if (form.NumberOfFloors < 1)
			{
				throw new ArgumentException("Số tầng phải lớn hơn hoặc bằng 1.");
			}
			// Reject missing property selection
			if (form.PropertyId <= 0)
			{
				throw new ArgumentException("Chưa chọn nhà trọ.");
			}
		}

		// Map Building entity to list DTO including manager display name
		private static BuildingDto Map(Building b)
		{
			// Project entity fields and manager name into list DTO
			return new BuildingDto
			{
				Id = b.Id,
				PropertyId = b.PropertyId,
				Name = b.Name,
				NumberOfFloors = b.NumberOfFloors,
				Description = b.Description,
				ManagerId = b.ManagerId,
				ManagerName = b.Manager?.FullName ?? "Chưa gán",
				IsActive = b.IsActive
			};
		}
	}
}
