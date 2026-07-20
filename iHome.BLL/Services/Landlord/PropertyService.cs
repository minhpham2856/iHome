using iHome.BLL.DTOs.Landlord;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services
{
	public class LandlordPropertyService
	{
		private readonly PropertyRepository _properties;
		private readonly AuditLogRepository _audits;

		public LandlordPropertyService() : this(new IHomeDbContext())
		{
		}

		public LandlordPropertyService(IHomeDbContext context)
		{
			_properties = new PropertyRepository(context);
			_audits = new AuditLogRepository(context);
		}

		public List<PropertyDto> GetByLandlord(int landlordId) =>
			_properties.GetByLandlord(landlordId).Select(Map).ToList();

		public PropertyFormDto? GetForm(int landlordId, int propertyId)
		{
			var property = RequireOwned(landlordId, propertyId);
			return new PropertyFormDto
			{
				Id = property.Id,
				Name = property.Name,
				Address = property.Address,
				Description = property.Description,
				IsActive = property.IsActive
			};
		}

		public void Create(int landlordId, PropertyFormDto form)
		{
			Validate(form);
			var entity = new Property
			{
				LandlordId = landlordId,
				Name = form.Name.Trim(),
				Address = form.Address.Trim(),
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
				IsActive = form.IsActive,
				CreatedAt = DateTime.Now
			};
			if (!_properties.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm nhà trọ.");
			}

			_audits.Add(
				landlordId,
				"Create",
				"Properties",
				entity.Id.ToString(),
				null,
				$"Name={entity.Name}; Address={entity.Address}; Active={entity.IsActive}");
		}

		public void Update(int landlordId, PropertyFormDto form)
		{
			Validate(form);
			var existing = RequireOwned(landlordId, form.Id);
			string oldValue =
				$"Name={existing.Name}; Address={existing.Address}; Active={existing.IsActive}";

			string name = form.Name.Trim();
			string address = form.Address.Trim();
			string? description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
			_properties.Update(new Property
			{
				Id = form.Id,
				Name = name,
				Address = address,
				Description = description,
				IsActive = form.IsActive
			});

			_audits.Add(
				landlordId,
				"Update",
				"Properties",
				form.Id.ToString(),
				oldValue,
				$"Name={name}; Address={address}; Active={form.IsActive}");
		}

		public void Deactivate(int landlordId, int propertyId)
		{
			var existing = RequireOwned(landlordId, propertyId);
			if (!_properties.Disable(propertyId))
			{
				throw new InvalidOperationException("Không thể ngừng hoạt động nhà trọ.");
			}

			_audits.Add(
				landlordId,
				"Disable",
				"Properties",
				propertyId.ToString(),
				$"Name={existing.Name}; Active=true",
				"Active=false");
		}

		private Property RequireOwned(int landlordId, int propertyId)
		{
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
			return property;
		}

		private static void Validate(PropertyFormDto form)
		{
			if (string.IsNullOrWhiteSpace(form.Name))
			{
				throw new ArgumentException("Tên nhà trọ không được để trống.");
			}
			if (string.IsNullOrWhiteSpace(form.Address))
			{
				throw new ArgumentException("Địa chỉ không được để trống.");
			}
		}

		private static PropertyDto Map(Property p) => new()
		{
			Id = p.Id,
			Name = p.Name,
			Address = p.Address,
			Description = p.Description,
			IsActive = p.IsActive,
			BuildingCount = p.Buildings?.Count ?? 0
		};
	}
}
