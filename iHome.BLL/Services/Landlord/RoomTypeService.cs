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
			_roomTypes = new RoomTypeRepository(context);
			_properties = new PropertyRepository(context);
			_audits = new AuditLogRepository(context);
		}

		// Room type grid filtered by property
		public List<RoomTypeDto> GetByProperty(int landlordId, int propertyId)
		{
			RequireOwnedProperty(landlordId, propertyId);
			return _roomTypes.GetByProperty(propertyId).Select(Map).ToList();
		}

		// Load edit form for a room type
		public RoomTypeFormDto? GetForm(int landlordId, int roomTypeId)
		{
			var roomType = RequireOwnedRoomType(landlordId, roomTypeId);
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
			Validate(form);
			RequireOwnedProperty(landlordId, form.PropertyId);

			var entity = new RoomType
			{
				PropertyId = form.PropertyId,
				TypeName = form.TypeName.Trim(),
				MaxOccupancy = form.MaxOccupancy,
				Area = form.Area,
				BaseRent = form.BaseRent,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim()
			};
			if (!_roomTypes.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm loại phòng.");
			}

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
			Validate(form);
			var existing = RequireOwnedRoomType(landlordId, form.Id);
			RequireOwnedProperty(landlordId, form.PropertyId);

			string typeName = form.TypeName.Trim();
			var before = new Dictionary<string, string?>
			{
				[AuditField.TypeName] = existing.TypeName,
				[AuditField.MaxOccupancy] = existing.MaxOccupancy.ToString(),
				[AuditField.Rent] = existing.BaseRent.ToString("N0"),
				[AuditField.Area] = existing.Area?.ToString()
			};
			var after = new Dictionary<string, string?>
			{
				[AuditField.TypeName] = typeName,
				[AuditField.MaxOccupancy] = form.MaxOccupancy.ToString(),
				[AuditField.Rent] = form.BaseRent.ToString("N0"),
				[AuditField.Area] = form.Area?.ToString()
			};
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			_roomTypes.Update(new RoomType
			{
				Id = form.Id,
				TypeName = typeName,
				MaxOccupancy = form.MaxOccupancy,
				Area = form.Area,
				BaseRent = form.BaseRent,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim()
			});

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
			var existing = RequireOwnedRoomType(landlordId, roomTypeId);
			if (_roomTypes.HasRooms(existing))
			{
				throw new InvalidOperationException("Không thể xóa loại phòng đang được sử dụng bởi phòng.");
			}
			if (!_roomTypes.Delete(existing))
			{
				throw new InvalidOperationException("Không thể xóa loại phòng.");
			}

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
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
			return property;
		}

		// Guard room type → property → landlord ownership chain
		private RoomType RequireOwnedRoomType(int landlordId, int roomTypeId)
		{
			var roomType = _roomTypes.GetById(roomTypeId);
			if (roomType == null || roomType.Property == null || roomType.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Loại phòng không thuộc về bạn.");
			}
			return roomType;
		}

		// Validate room type form fields
		private static void Validate(RoomTypeFormDto form)
		{
			if (string.IsNullOrWhiteSpace(form.TypeName))
			{
				throw new ArgumentException("Tên loại phòng không được để trống.");
			}
			if (form.MaxOccupancy < 1)
			{
				throw new ArgumentException("Số người tối đa phải lớn hơn hoặc bằng 1.");
			}
			if (form.BaseRent < 0)
			{
				throw new ArgumentException("Giá thuê không hợp lệ.");
			}
			if (form.Area.HasValue && form.Area.Value < 0)
			{
				throw new ArgumentException("Diện tích không hợp lệ.");
			}
			if (form.PropertyId <= 0)
			{
				throw new ArgumentException("Chưa chọn nhà trọ.");
			}
		}

		// Map RoomType entity to list DTO including room count
		private static RoomTypeDto Map(RoomType rt)
		{
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
