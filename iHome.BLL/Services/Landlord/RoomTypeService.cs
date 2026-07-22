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
	// CRUD room types (RoomType) per property — BaseRent/MaxOccupancy used when creating Room
	public class LandlordRoomTypeService
	{
		private readonly RoomTypeRepository _roomTypes;
		private readonly PropertyRepository _properties;
		private readonly AuditLogRepository _audits;

		public LandlordRoomTypeService() : this(new IHomeDbContext()) { }

		public LandlordRoomTypeService(IHomeDbContext context)
		{
			// Room type data access against shared DbContext
			_roomTypes = new RoomTypeRepository(context);
			// Property ownership checks
			_properties = new PropertyRepository(context);
			// Audit trail for create/update/delete operations
			_audits = new AuditLogRepository(context);
		}

		// Room type grid filtered by property
		public List<RoomTypeDto> GetByProperty(int landlordId, int propertyId)
		{
			// Guard property ownership before querying room types
			RequireOwnedProperty(landlordId, propertyId);
			// Load room types for property and map to list DTOs
			return _roomTypes.GetByProperty(propertyId).Select(Map).ToList();
		}

		// Load edit form for a room type
		public RoomTypeFormDto? GetForm(int landlordId, int roomTypeId)
		{
			// Verify room type belongs to landlord via property chain
			var roomType = RequireOwnedRoomType(landlordId, roomTypeId);
			// Project entity fields into form DTO for two-way binding
			return new RoomTypeFormDto
			{
				Id = roomType.Id,
				PropertyId = roomType.PropertyId,
				TypeName = roomType.TypeName,
				MaxOccupancy = roomType.MaxOccupancy,
				Area = roomType.Area,
				BaseRent = roomType.BaseRent,
				Description = roomType.Description
			};
		}

		// Create a new room type
		public void Create(int landlordId, RoomTypeFormDto form)
		{
			// Validate required form fields
			Validate(form);
			// Verify property ownership
			RequireOwnedProperty(landlordId, form.PropertyId);

			// Build new RoomType entity from trimmed form values
			var entity = new RoomType
			{
				PropertyId = form.PropertyId,
				TypeName = form.TypeName.Trim(),
				MaxOccupancy = form.MaxOccupancy,
				Area = form.Area,
				BaseRent = form.BaseRent,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim()
			};
			// INSERT; throw if repository reports failure
			if (!_roomTypes.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm loại phòng.");
			}

			// Record create audit with new-value snapshot
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Create,
				TableName = AuditObject.RoomTypes,
				RecordId = entity.Id.ToString(),
				Detail = entity.TypeName,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.TypeName] = entity.TypeName,
					[AuditField.PropertyId] = entity.PropertyId.ToString(),
					[AuditField.Rent] = entity.BaseRent.ToString("N0")
				}),
				Timestamp = DateTime.Now
			});
		}

		// Update — PropertyId not changed on entity update (ownership validated only)
		public void Update(int landlordId, RoomTypeFormDto form)
		{
			// Validate required form fields
			Validate(form);
			// Load existing room type and verify ownership
			var existing = RequireOwnedRoomType(landlordId, form.Id);
			// Verify property ownership for form property id
			RequireOwnedProperty(landlordId, form.PropertyId);

			// Normalize type name
			string typeName = form.TypeName.Trim();
			// Snapshot values before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.TypeName] = existing.TypeName,
				[AuditField.MaxOccupancy] = existing.MaxOccupancy.ToString(),
				[AuditField.Rent] = existing.BaseRent.ToString("N0"),
				[AuditField.Area] = existing.Area?.ToString()
			};
			// Snapshot values after edit for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.TypeName] = typeName,
				[AuditField.MaxOccupancy] = form.MaxOccupancy.ToString(),
				[AuditField.Rent] = form.BaseRent.ToString("N0"),
				[AuditField.Area] = form.Area?.ToString()
			};
			// Build compact old/new diff strings
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			// Persist updated room type row
			_roomTypes.Update(new RoomType
			{
				Id = form.Id,
				TypeName = typeName,
				MaxOccupancy = form.MaxOccupancy,
				Area = form.Area,
				BaseRent = form.BaseRent,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim()
			});

			// Write update audit entry
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Update,
				TableName = AuditObject.RoomTypes,
				RecordId = form.Id.ToString(),
				Detail = typeName,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Delete only when no Room references remain
		public void Delete(int landlordId, int roomTypeId)
		{
			// Load room type and verify ownership
			var existing = RequireOwnedRoomType(landlordId, roomTypeId);
			// Block delete when rooms still reference this type
			if (_roomTypes.HasRooms(existing))
			{
				throw new InvalidOperationException("Không thể xóa loại phòng đang được sử dụng bởi phòng.");
			}
			// DELETE; throw if repository reports failure
			if (!_roomTypes.Delete(existing))
			{
				throw new InvalidOperationException("Không thể xóa loại phòng.");
			}

			// Record delete audit with old-value snapshot
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Delete,
				TableName = AuditObject.RoomTypes,
				RecordId = roomTypeId.ToString(),
				Detail = existing.TypeName,
				OldValue = AuditDiff.FormatOldOnly(new Dictionary<string, string?>
				{
					[AuditField.TypeName] = existing.TypeName,
					[AuditField.PropertyId] = existing.PropertyId.ToString()
				}),
				NewValue = null,
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

		// Guard room type → property → landlord ownership chain
		private RoomType RequireOwnedRoomType(int landlordId, int roomTypeId)
		{
			// Load room type with navigation to property
			var roomType = _roomTypes.GetById(roomTypeId);
			// Reject missing room type or property not owned by landlord
			if (roomType == null || roomType.Property == null || roomType.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Loại phòng không thuộc về bạn.");
			}
			// Return verified entity to caller
			return roomType;
		}

		// Validate room type form fields
		private static void Validate(RoomTypeFormDto form)
		{
			// Reject empty type name
			if (string.IsNullOrWhiteSpace(form.TypeName))
			{
				throw new ArgumentException("Tên loại phòng không được để trống.");
			}
			// Reject invalid max occupancy
			if (form.MaxOccupancy < 1)
			{
				throw new ArgumentException("Số người tối đa phải lớn hơn hoặc bằng 1.");
			}
			// Reject negative base rent
			if (form.BaseRent < 0)
			{
				throw new ArgumentException("Giá thuê không hợp lệ.");
			}
			// Reject negative area when provided
			if (form.Area.HasValue && form.Area.Value < 0)
			{
				throw new ArgumentException("Diện tích không hợp lệ.");
			}
			// Reject missing property selection
			if (form.PropertyId <= 0)
			{
				throw new ArgumentException("Chưa chọn nhà trọ.");
			}
		}

		// Map RoomType entity to list DTO including room count
		private static RoomTypeDto Map(RoomType rt)
		{
			// Project entity fields and related room count into list DTO
			return new RoomTypeDto
			{
				Id = rt.Id,
				PropertyId = rt.PropertyId,
				TypeName = rt.TypeName,
				MaxOccupancy = rt.MaxOccupancy,
				Area = rt.Area,
				BaseRent = rt.BaseRent,
				Description = rt.Description,
				RoomCount = rt.Rooms?.Count ?? 0
			};
		}
	}
}
