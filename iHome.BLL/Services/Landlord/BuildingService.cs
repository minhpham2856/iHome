using iHome.BLL.DTOs.Landlord;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services
{
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

		public List<BuildingDto> GetByProperty(int landlordId, int propertyId)
		{
			RequireOwnedProperty(landlordId, propertyId);
			return _buildings.GetByProperty(propertyId).Select(Map).ToList();
		}

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

		public List<ManagerOptionDto> GetManagerOptions(int propertyId)
		{
			var options = new List<ManagerOptionDto>
			{
				new() { Id = null, Name = "Chưa gán" }
			};

			options.AddRange(_users.GetManagersForProperty(propertyId).Select(m => new ManagerOptionDto
			{
				Id = m.Id,
				Name = m.FullName
			}));
			return options;
		}

		public void Create(int landlordId, BuildingFormDto form)
		{
			Validate(form);
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			ValidateManager(form.ManagerId, form.PropertyId);

			// inactive property cannot host an active building
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

			_audits.Add(
				landlordId,
				"Create",
				"Buildings",
				entity.Id.ToString(),
				null,
				$"Name={entity.Name}; PropertyId={entity.PropertyId}; ManagerId={entity.ManagerId}; Active={entity.IsActive}");
		}

		public void Update(int landlordId, BuildingFormDto form)
		{
			Validate(form);
			var existing = RequireOwnedBuilding(landlordId, form.Id);
			var property = RequireOwnedProperty(landlordId, form.PropertyId);
			ValidateManager(form.ManagerId, form.PropertyId);

			string oldValue =
				$"Name={existing.Name}; Floors={existing.NumberOfFloors}; ManagerId={existing.ManagerId}; Active={existing.IsActive}";

			// inactive property cannot host an active building
			bool isActive = property.IsActive && form.IsActive;
			string name = form.Name.Trim();
			_buildings.Update(new Building
			{
				Id = form.Id,
				Name = name,
				NumberOfFloors = form.NumberOfFloors,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
				ManagerId = form.ManagerId,
				IsActive = isActive
			});

			_audits.Add(
				landlordId,
				"Update",
				"Buildings",
				form.Id.ToString(),
				oldValue,
				$"Name={name}; Floors={form.NumberOfFloors}; ManagerId={form.ManagerId}; Active={isActive}");
		}

		public void Deactivate(int landlordId, int buildingId)
		{
			var existing = RequireOwnedBuilding(landlordId, buildingId);
			if (!_buildings.Disable(buildingId))
			{
				throw new InvalidOperationException("Không thể ngừng hoạt động tòa nhà.");
			}

			_audits.Add(
				landlordId,
				"Disable",
				"Buildings",
				buildingId.ToString(),
				$"Name={existing.Name}; Active=true",
				"Active=false");
		}

		private Property RequireOwnedProperty(int landlordId, int propertyId)
		{
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
			return property;
		}

		private Building RequireOwnedBuilding(int landlordId, int buildingId)
		{
			var building = _buildings.GetById(buildingId);
			if (building == null || building.Property == null || building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Tòa nhà không thuộc về bạn.");
			}
			return building;
		}

		private void ValidateManager(int? managerId, int propertyId)
		{
			if (!managerId.HasValue) return;
			var manager = _users.GetById(managerId.Value);
			if (manager == null || manager.Role != "Manager" || !manager.IsActive)
			{
				throw new ArgumentException("Quản lý được chọn không hợp lệ.");
			}
			if (manager.ManagedPropertyId != propertyId)
			{
				throw new ArgumentException("Nhân viên này được gán nhà trọ khác.");
			}
		}

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

		private static BuildingDto Map(Building b) => new()
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
