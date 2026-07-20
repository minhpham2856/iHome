using iHome.BLL.DTOs.Landlord;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services
{
	public class LandlordRoomTypeService
	{
		private readonly RoomTypeRepository _roomTypes;
		private readonly PropertyRepository _properties;
		private readonly AuditLogRepository _audits;

		public LandlordRoomTypeService() : this(new IHomeDbContext())
		{
		}

		public LandlordRoomTypeService(IHomeDbContext context)
		{
			_roomTypes = new RoomTypeRepository(context);
			_properties = new PropertyRepository(context);
			_audits = new AuditLogRepository(context);
		}

		public List<RoomTypeDto> GetByProperty(int landlordId, int propertyId)
		{
			RequireOwnedProperty(landlordId, propertyId);
			return _roomTypes.GetByProperty(propertyId).Select(Map).ToList();
		}

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

			_audits.Add(
				landlordId,
				"Create",
				"RoomTypes",
				entity.Id.ToString(),
				null,
				$"Name={entity.TypeName}; PropertyId={entity.PropertyId}; Rent={entity.BaseRent}");
		}

		public void Update(int landlordId, RoomTypeFormDto form)
		{
			Validate(form);
			var existing = RequireOwnedRoomType(landlordId, form.Id);
			RequireOwnedProperty(landlordId, form.PropertyId);

			string oldValue =
				$"Name={existing.TypeName}; Max={existing.MaxOccupancy}; Rent={existing.BaseRent}; Area={existing.Area}";
			string typeName = form.TypeName.Trim();

			_roomTypes.Update(new RoomType
			{
				Id = form.Id,
				TypeName = typeName,
				MaxOccupancy = form.MaxOccupancy,
				Area = form.Area,
				BaseRent = form.BaseRent,
				Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim()
			});

			_audits.Add(
				landlordId,
				"Update",
				"RoomTypes",
				form.Id.ToString(),
				oldValue,
				$"Name={typeName}; Max={form.MaxOccupancy}; Rent={form.BaseRent}; Area={form.Area}");
		}

		public void Delete(int landlordId, int roomTypeId)
		{
			var existing = RequireOwnedRoomType(landlordId, roomTypeId);
			if (_roomTypes.HasRooms(roomTypeId))
			{
				throw new InvalidOperationException("Không thể xóa loại phòng đang được sử dụng bởi phòng.");
			}
			if (!_roomTypes.Delete(roomTypeId))
			{
				throw new InvalidOperationException("Không thể xóa loại phòng.");
			}

			_audits.Add(
				landlordId,
				"Delete",
				"RoomTypes",
				roomTypeId.ToString(),
				$"Name={existing.TypeName}; PropertyId={existing.PropertyId}",
				null);
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

		private RoomType RequireOwnedRoomType(int landlordId, int roomTypeId)
		{
			var roomType = _roomTypes.GetById(roomTypeId);
			if (roomType == null || roomType.Property == null || roomType.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Loại phòng không thuộc về bạn.");
			}
			return roomType;
		}

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

		private static RoomTypeDto Map(RoomType rt) => new()
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
