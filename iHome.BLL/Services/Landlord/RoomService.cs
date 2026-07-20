using iHome.BLL.DTOs.Landlord;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services
{
	public class LandlordRoomService
	{
		private const string StatusEmpty = "Empty";
		private const string StatusOccupied = "Occupied";
		private const string StatusDeposited = "Deposited";
		private const string ContractActive = "Active";

		private readonly RoomRepository _rooms;
		private readonly RoomTypeRepository _roomTypes;
		private readonly BuildingRepository _buildings;
		private readonly PropertyRepository _properties;
		private readonly AuditLogRepository _audits;

		public LandlordRoomService() : this(new IHomeDbContext())
		{
		}

		public LandlordRoomService(IHomeDbContext context)
		{
			_rooms = new RoomRepository(context);
			_roomTypes = new RoomTypeRepository(context);
			_buildings = new BuildingRepository(context);
			_properties = new PropertyRepository(context);
			_audits = new AuditLogRepository(context);
		}

		public List<PropertyFilterOptionDto> GetPropertyOptions(int landlordId) =>
			_properties.GetByLandlord(landlordId)
				.Where(p => p.IsActive)
				.Select(p => new PropertyFilterOptionDto { Id = p.Id, Name = p.Name })
				.ToList();

		public List<BuildingFilterOptionDto> GetBuildingOptions(int landlordId, int propertyId)
		{
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}

			return _buildings.GetByProperty(propertyId)
				.Where(b => b.IsActive)
				.Select(b => new BuildingFilterOptionDto
				{
					Id = b.Id,
					PropertyId = b.PropertyId,
					Name = b.Name,
					NumberOfFloors = b.NumberOfFloors
				})
				.ToList();
		}

		public List<RoomTypeOptionDto> GetRoomTypeOptions(int landlordId, int propertyId)
		{
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}

			return _roomTypes.GetByProperty(propertyId)
				.Select(rt => new RoomTypeOptionDto
				{
					Id = rt.Id,
					Name = rt.TypeName,
					BaseRent = rt.BaseRent,
					Area = rt.Area
				})
				.ToList();
		}

		public List<RoomDto> GetByBuilding(int landlordId, int buildingId)
		{
			RequireOwnedBuilding(landlordId, buildingId);
			return _rooms.GetByBuilding(buildingId).Select(MapList).ToList();
		}

		public RoomFormDto? GetForm(int landlordId, int roomId)
		{
			var room = RequireOwnedRoom(landlordId, roomId);
			return new RoomFormDto
			{
				Id = room.Id,
				BuildingId = room.BuildingId,
				RoomTypeId = room.RoomTypeId,
				RoomNumber = room.RoomNumber,
				Floor = room.Floor,
				Status = room.Status,
				Notes = room.Notes
			};
		}

		public RoomDetailDto GetDetail(int landlordId, int roomId)
		{
			var room = RequireOwnedRoom(landlordId, roomId);
			var tenants = room.Contracts
				.Where(c => c.Status == ContractActive)
				.SelectMany(c => c.ContractTenants)
				.GroupBy(ct => ct.TenantId)
				.Select(g => g.First())
				.Select(ct => new RoomTenantDto
				{
					TenantId = ct.TenantId,
					FullName = ct.Tenant?.FullName ?? string.Empty,
					PhoneNumber = ct.Tenant?.PhoneNumber ?? string.Empty,
					IdCardNumber = ct.Tenant?.IdCardNumber ?? string.Empty,
					Email = ct.Tenant?.Email,
					IsMainTenant = ct.IsMainTenant
				})
				.OrderByDescending(t => t.IsMainTenant)
				.ThenBy(t => t.FullName)
				.ToList();

			return new RoomDetailDto
			{
				Id = room.Id,
				BuildingName = room.Building?.Name ?? string.Empty,
				PropertyName = room.Building?.Property?.Name ?? string.Empty,
				RoomNumber = room.RoomNumber,
				RoomTypeName = room.RoomType?.TypeName ?? string.Empty,
				Floor = room.Floor,
				Area = room.RoomType?.Area,
				BaseRent = room.RoomType?.BaseRent ?? 0,
				Status = room.Status,
				StatusDisplay = FormatStatus(room.Status),
				MaxOccupancy = room.RoomType?.MaxOccupancy ?? 0,
				Notes = room.Notes,
				Tenants = tenants
			};
		}

		public void Create(int landlordId, RoomFormDto form)
		{
			Validate(form);
			var building = RequireOwnedBuilding(landlordId, form.BuildingId);
			RequireRoomTypeForProperty(form.RoomTypeId, building.PropertyId);
			EnsureFloor(form.Floor, building.NumberOfFloors);
			EnsureUniqueRoomNumber(form.BuildingId, form.RoomNumber, null);

			var entity = new Room
			{
				BuildingId = form.BuildingId,
				RoomTypeId = form.RoomTypeId,
				RoomNumber = form.RoomNumber.Trim(),
				Floor = form.Floor,
				Status = NormalizeStatus(form.Status),
				Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
			};
			if (!_rooms.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm phòng.");
			}

			_audits.Add(
				landlordId,
				"Create",
				"Rooms",
				entity.Id.ToString(),
				null,
				$"BuildingId={entity.BuildingId}; Room={entity.RoomNumber}; Status={entity.Status}");
		}

		public void Update(int landlordId, RoomFormDto form)
		{
			Validate(form);
			var room = RequireOwnedRoom(landlordId, form.Id);
			var building = RequireOwnedBuilding(landlordId, form.BuildingId);
			RequireRoomTypeForProperty(form.RoomTypeId, building.PropertyId);
			EnsureFloor(form.Floor, building.NumberOfFloors);
			EnsureUniqueRoomNumber(form.BuildingId, form.RoomNumber, form.Id);

			string oldValue =
				$"Room={room.RoomNumber}; Floor={room.Floor}; TypeId={room.RoomTypeId}; Status={room.Status}";

			int occupancy = CountOccupancy(room);
			string status = ResolveStatus(form.Status, occupancy > 0);
			string roomNumber = form.RoomNumber.Trim();

			_rooms.Update(new Room
			{
				Id = form.Id,
				RoomTypeId = form.RoomTypeId,
				RoomNumber = roomNumber,
				Floor = form.Floor,
				Status = status,
				Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
			});

			_audits.Add(
				landlordId,
				"Update",
				"Rooms",
				form.Id.ToString(),
				oldValue,
				$"Room={roomNumber}; Floor={form.Floor}; TypeId={form.RoomTypeId}; Status={status}");
		}

		public void Delete(int landlordId, int roomId)
		{
			var room = RequireOwnedRoom(landlordId, roomId);
			if (_rooms.HasContracts(roomId))
			{
				throw new InvalidOperationException("Không thể xóa phòng đã có hợp đồng.");
			}
			if (!_rooms.Delete(roomId))
			{
				throw new InvalidOperationException("Không thể xóa phòng.");
			}

			_audits.Add(
				landlordId,
				"Delete",
				"Rooms",
				roomId.ToString(),
				$"BuildingId={room.BuildingId}; Room={room.RoomNumber}",
				null);
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

		private Room RequireOwnedRoom(int landlordId, int roomId)
		{
			var room = _rooms.GetById(roomId);
			if (room == null || room.Building?.Property == null || room.Building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Phòng không thuộc về bạn.");
			}
			return room;
		}

		private void RequireRoomTypeForProperty(int roomTypeId, int propertyId)
		{
			var roomType = _roomTypes.GetById(roomTypeId);
			if (roomType == null || roomType.PropertyId != propertyId)
			{
				throw new ArgumentException("Loại phòng không thuộc nhà trọ đã chọn.");
			}
		}

		private void EnsureUniqueRoomNumber(int buildingId, string roomNumber, int? excludeRoomId)
		{
			if (_rooms.RoomNumberExists(buildingId, roomNumber, excludeRoomId))
			{
				throw new ArgumentException("Số phòng đã tồn tại trong tòa nhà này.");
			}
		}

		private static void EnsureFloor(int floor, int numberOfFloors)
		{
			if (floor < 1 || floor > numberOfFloors)
			{
				throw new ArgumentException($"Tầng phải từ 1 đến {numberOfFloors}.");
			}
		}

		private static void Validate(RoomFormDto form)
		{
			if (form.BuildingId <= 0)
			{
				throw new ArgumentException("Chưa chọn tòa nhà.");
			}
			if (form.RoomTypeId <= 0)
			{
				throw new ArgumentException("Chưa chọn loại phòng.");
			}
			if (string.IsNullOrWhiteSpace(form.RoomNumber))
			{
				throw new ArgumentException("Số phòng không được để trống.");
			}
			if (form.Floor < 1)
			{
				throw new ArgumentException("Tầng phải lớn hơn hoặc bằng 1.");
			}
		}

		private static string NormalizeStatus(string status)
		{
			if (string.Equals(status, StatusOccupied, StringComparison.OrdinalIgnoreCase))
			{
				return StatusOccupied;
			}
			if (string.Equals(status, StatusDeposited, StringComparison.OrdinalIgnoreCase))
			{
				return StatusDeposited;
			}
			return StatusEmpty;
		}

		private static string ResolveStatus(string requested, bool hasOccupants)
		{
			if (hasOccupants)
			{
				return StatusOccupied;
			}
			return NormalizeStatus(requested);
		}

		private static string FormatStatus(string status) => status switch
		{
			StatusOccupied => "Đang ở",
			StatusDeposited => "Đã đặt cọc",
			_ => "Còn trống"
		};

		private static int CountOccupancy(Room r) =>
			r.Contracts?
				.Where(c => c.Status == ContractActive)
				.SelectMany(c => c.ContractTenants)
				.Select(ct => ct.TenantId)
				.Distinct()
				.Count() ?? 0;

		private static RoomDto MapList(Room r)
		{
			int occupancy = CountOccupancy(r);

			return new RoomDto
			{
				Id = r.Id,
				BuildingId = r.BuildingId,
				RoomTypeId = r.RoomTypeId,
				RoomTypeName = r.RoomType?.TypeName ?? string.Empty,
				RoomNumber = r.RoomNumber,
				Floor = r.Floor,
				Area = r.RoomType?.Area,
				BaseRent = r.RoomType?.BaseRent ?? 0,
				Status = r.Status,
				StatusDisplay = FormatStatus(r.Status),
				CurrentOccupancy = occupancy,
				MaxOccupancy = r.RoomType?.MaxOccupancy ?? 0,
				Notes = r.Notes
			};
		}
	}
}
