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
			// Property data access against shared DbContext
			_properties = new PropertyRepository(context);
			// Audit trail for create/update operations
			_audits = new AuditLogRepository(context);
		}

		// List properties belonging to the landlord for the grid view
		public List<PropertyDto> GetByLandlord(int landlordId)
		{
			// Load all properties for this landlord from repository
			var properties = _properties.GetByLandlord(landlordId);
			// Map each entity to list DTO and return materialized list
			return properties.Select(Map).ToList();
		}

		// Load edit form — throws from RequireOwned if property does not belong to landlord
		public PropertyFormDto? GetForm(int landlordId, int propertyId)
		{
			// Verify ownership and load entity
			var property = RequireOwned(landlordId, propertyId);
			// Project entity fields into form DTO for two-way binding
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
			// Validate required form fields
			Validate(form);
			// Build new Property entity from trimmed form values
			var entity = new Property
			{
				LandlordId = landlordId,
				Name = form.Name.Trim(),
				Address = form.Address.Trim(),
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
				IsActive = form.IsActive,
				CreatedAt = DateTime.Now
			};
			// INSERT; throw if repository reports failure
			if (!_properties.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm nhà trọ.");
			}

			// Record create audit with new-value snapshot
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
			// Validate form input
			Validate(form);
			// Load existing row and verify landlord ownership
			var existing = RequireOwned(landlordId, form.Id);

			// Normalize editable scalar fields
			string name = form.Name.Trim();
			string address = form.Address.Trim();
			string? description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
			// Snapshot values before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.Name] = existing.Name,
				[AuditField.Address] = existing.Address,
				[AuditField.Active] = DisplayText.FormatActive(existing.IsActive)
			};
			// Snapshot values after edit for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.Name] = name,
				[AuditField.Address] = address,
				[AuditField.Active] = DisplayText.FormatActive(form.IsActive)
			};
			// Build compact old/new diff strings
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			// Persist updated property row
			_properties.Update(new Property
			{
				Id = form.Id,
				Name = name,
				Address = address,
				Description = description,
				IsActive = form.IsActive
			});

			// Write update audit entry
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

		// Guard ownership — property.LandlordId must match caller
		private Property RequireOwned(int landlordId, int propertyId)
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

		// Validate form: Name and Address are required
		private static void Validate(PropertyFormDto form)
		{
			// Reject empty property name
			if (string.IsNullOrWhiteSpace(form.Name))
			{
				throw new ArgumentException("Tên nhà trọ không được để trống.");
			}
			// Reject empty address
			if (string.IsNullOrWhiteSpace(form.Address))
			{
				throw new ArgumentException("Địa chỉ không được để trống.");
			}
		}

		// Map Property entity to list DTO including BuildingCount
		private static PropertyDto Map(Property p)
		{
			// Project entity scalar fields and related building count into list DTO
			return new PropertyDto
			{
				Id = p.Id,
				Name = p.Name,
				Address = p.Address,
				Description = p.Description,
				IsActive = p.IsActive,
				// Count related buildings for grid summary column
				BuildingCount = p.Buildings?.Count ?? 0
			};
		}
	}
}
