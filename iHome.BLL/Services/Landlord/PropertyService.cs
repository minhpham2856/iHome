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
	// CRUD rental properties (Property) owned by a Landlord — every change writes an AuditLog entry
	public class LandlordPropertyService
	{
		private readonly PropertyRepository _properties;
		private readonly AuditLogRepository _audits;

		public LandlordPropertyService() : this(new IHomeDbContext()) { }

		public LandlordPropertyService(IHomeDbContext context)
		{
			_properties = new PropertyRepository(context);
			_audits = new AuditLogRepository(context);
		}

		// List properties belonging to the landlord for the grid view
		public List<PropertyDto> GetByLandlord(int landlordId)
		{
			var properties = _properties.GetByLandlord(landlordId);
			return properties.Select(Map).ToList();
		}

		// Load edit form — throws from RequireOwned if property does not belong to landlord
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

		// Create a new property with LandlordId set to the caller
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

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Create,
				TableName = AuditObject.Properties,
				RecordId = entity.Id.ToString(),
				Detail = entity.Name,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.Name] = entity.Name,
					[AuditField.Address] = entity.Address,
					[AuditField.Active] = DisplayText.FormatActive(entity.IsActive)
				}),
				Timestamp = DateTime.Now
			});
		}

		// Update property — AuditDiff captures before/after for key fields
		public void Update(int landlordId, PropertyFormDto form)
		{
			Validate(form);
			var existing = RequireOwned(landlordId, form.Id);

			string name = form.Name.Trim();
			string address = form.Address.Trim();
			string? description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
			var before = new Dictionary<string, string?>
			{
				[AuditField.Name] = existing.Name,
				[AuditField.Address] = existing.Address,
				[AuditField.Active] = DisplayText.FormatActive(existing.IsActive)
			};
			var after = new Dictionary<string, string?>
			{
				[AuditField.Name] = name,
				[AuditField.Address] = address,
				[AuditField.Active] = DisplayText.FormatActive(form.IsActive)
			};
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			_properties.Update(new Property
			{
				Id = form.Id,
				Name = name,
				Address = address,
				Description = description,
				IsActive = form.IsActive
			});

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Update,
				TableName = AuditObject.Properties,
				RecordId = form.Id.ToString(),
				Detail = name,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Ensure the property belongs to this landlord
		private Property RequireOwned(int landlordId, int propertyId)
		{
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
			return property;
		}

		// Validate form: Name and Address are required
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

		// Map Property entity to list DTO including BuildingCount
		private static PropertyDto Map(Property p)
		{
			return new PropertyDto
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
}
