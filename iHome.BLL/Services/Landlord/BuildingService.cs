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
			_buildings = new BuildingRepository(context);
			_properties = new PropertyRepository(context);
			_users = new UserRepository(context);
			_audits = new AuditLogRepository(context);
		}

		// List buildings by property — verify property belongs to landlord first
		public List<BuildingDto> GetByProperty(int landlordId, int propertyId)
		{
			RequireOwnedProperty(landlordId, propertyId);
			return _buildings.GetByProperty(propertyId).Select(Map).ToList();
		}

		// Edit form — load ManagerId, IsActive, and floor count
		public BuildingFormDto? GetForm(int landlordId, int buildingId)
		{
			var building = RequireOwnedBuilding(landlordId, buildingId);
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

		// Manager combo for building assignment — all managers under this landlord
		public List<ManagerOptionDto> GetManagerOptions(int landlordId)
		{
			var options = new List<ManagerOptionDto>
			{
				new() { Id = null, Name = "Chưa gán" }
			};

			options.AddRange(_users.GetManagersForLandlord(landlordId)
				.Where(m => m.IsActive)
				.Select(m => new ManagerOptionDto
				{
					Id = m.Id,
					Name = m.FullName
				}));
			return options;
		}

		// Create new building — IsActive forced false when parent property is inactive
		public void Create(int landlordId, BuildingFormDto form)
		{
			Validate(form);
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			ValidateManager(form.ManagerId, landlordId);

			bool isActive = property.IsActive && form.IsActive;
			var entity = new Building
			{
				PropertyId = form.PropertyId,
				Name = form.Name.Trim(),
				NumberOfFloors = form.NumberOfFloors,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
				ManagerId = form.ManagerId,
				IsActive = isActive
			};
			if (!_buildings.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm tòa nhà.");
			}

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
			Validate(form);
			var existing = RequireOwnedBuilding(landlordId, form.Id);
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			ValidateManager(form.ManagerId, landlordId);

			bool isActive = property.IsActive && form.IsActive;
			string name = form.Name.Trim();
			var before = new Dictionary<string, string?>
			{
				[AuditField.Name] = existing.Name,
				[AuditField.Floors] = existing.NumberOfFloors.ToString(),
				[AuditField.ManagerId] = existing.ManagerId?.ToString(),
				[AuditField.Active] = DisplayText.FormatActive(existing.IsActive)
			};
			var after = new Dictionary<string, string?>
			{
				[AuditField.Name] = name,
				[AuditField.Floors] = form.NumberOfFloors.ToString(),
				[AuditField.ManagerId] = form.ManagerId?.ToString(),
				[AuditField.Active] = DisplayText.FormatActive(isActive)
			};
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			_buildings.Update(new Building
			{
				Id = form.Id,
				Name = name,
				NumberOfFloors = form.NumberOfFloors,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
				ManagerId = form.ManagerId,
				IsActive = isActive
			});

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
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
			return property;
		}

		// Guard building → property → landlord ownership chain
		private Building RequireOwnedBuilding(int landlordId, int buildingId)
		{
			var building = _buildings.GetById(buildingId);
			if (building == null || building.Property == null || building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Tòa nhà không thuộc về bạn.");
			}
			return building;
		}

		// Manager must have Role=Manager, be active, and belong to one of landlord's properties
		private void ValidateManager(int? managerId, int landlordId)
		{
			if (!managerId.HasValue) return;
			var manager = _users.GetById(managerId.Value);
			if (manager == null || manager.Role != UserRole.Manager || !manager.IsActive)
			{
				throw new ArgumentException("Quản lý được chọn không hợp lệ.");
			}
			bool underLandlord = _users.GetManagersForLandlord(landlordId)
				.Any(m => m.Id == managerId.Value);
			if (!underLandlord)
			{
				throw new ArgumentException("Nhân viên này không thuộc nhà trọ của bạn.");
			}
		}

		// Basic form validation
		private static void Validate(BuildingFormDto form)
		{
			if (string.IsNullOrWhiteSpace(form.Name))
			{
				throw new ArgumentException("Tên tòa nhà không được để trống.");
			}
			if (form.NumberOfFloors < 1)
			{
				throw new ArgumentException("Số tầng phải lớn hơn hoặc bằng 1.");
			}
			if (form.PropertyId <= 0)
			{
				throw new ArgumentException("Chưa chọn nhà trọ.");
			}
		}

		// Map Building entity to list DTO including manager display name
		private static BuildingDto Map(Building b)
		{
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
