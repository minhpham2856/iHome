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
	// CRUD rooms (Room) per building — auto force Occupied when active contract exists, full audit trail
	public class LandlordRoomService
	{
		private readonly RoomRepository _rooms;
		private readonly RoomTypeRepository _roomTypes;
		private readonly BuildingRepository _buildings;
		private readonly PropertyRepository _properties;
		private readonly AuditLogRepository _audits;

		public LandlordRoomService() : this(new IHomeDbContext()) { }

		public LandlordRoomService(IHomeDbContext context)
		{
			_rooms = new RoomRepository(context);
			_roomTypes = new RoomTypeRepository(context);
			_buildings = new BuildingRepository(context);
			_properties = new PropertyRepository(context);
			_audits = new AuditLogRepository(context);
		}

		// Active property combo for landlord filter
		public List<PropertyFilterOptionDto> GetPropertyOptions(int landlordId)
		{
			return _properties.GetByLandlord(landlordId)
				.Where(p => p.IsActive)
				.Select(p => new PropertyFilterOptionDto { Id = p.Id, Name = p.Name })
				.ToList();
		}

		// Active building combo filtered by property
		public List<BuildingFilterOptionDto> GetBuildingOptions(int landlordId, int propertyId)
		{
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}

			var options = new List<BuildingFilterOptionDto>
			{
				new() { Id = 0, PropertyId = propertyId, Name = "Tất cả" }
			};
			options.AddRange(_buildings.GetByProperty(propertyId)
				.Where(b => b.IsActive)
				.Select(b => new BuildingFilterOptionDto
				{
					Id = b.Id,
					PropertyId = b.PropertyId,
					Name = b.Name,
					NumberOfFloors = b.NumberOfFloors
				}));
			return options;
		}

		// Room type combo scoped to property
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

		// Room grid filtered by building
		public List<RoomDto> GetByBuilding(int landlordId, int buildingId)
		{
			RequireOwnedBuilding(landlordId, buildingId);
			return _rooms.GetByBuilding(buildingId).Select(MapList).ToList();
		}

		// Room grid for every building under a property
		public List<RoomDto> GetByProperty(int landlordId, int propertyId)
		{
			RequireOwnedProperty(landlordId, propertyId);
			return _rooms.GetByProperty(propertyId).Select(MapList).ToList();
		}

		// Create/edit form for a room
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

		// Room detail plus tenant list from Active contracts (distinct TenantId)
		public RoomDetailDto GetDetail(int landlordId, int roomId)
		{
			var room = RequireOwnedRoom(landlordId, roomId);
			var tenants = room.Contracts
				.Where(c => c.Status == ContractStatus.Active)
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
				StatusDisplay = RoomStatus.Format(room.Status),
				MaxOccupancy = room.RoomType?.MaxOccupancy ?? 0,
				Notes = room.Notes,
				Tenants = tenants
			};
		}

		// Create new room — validate floor, unique room number, room type belongs to property
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

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Create,
				TableName = AuditObject.Rooms,
				RecordId = entity.Id.ToString(),
				Detail = entity.RoomNumber,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.BuildingId] = entity.BuildingId.ToString(),
					[AuditField.RoomNumber] = entity.RoomNumber,
					[AuditField.Status] = RoomStatus.Format(entity.Status)
				}),
				Timestamp = DateTime.Now
			});
		}

		// Update — force Status=Occupied when room has occupants regardless of form
		public void Update(int landlordId, RoomFormDto form)
		{
			Validate(form);
			var room = RequireOwnedRoom(landlordId, form.Id);
			var building = RequireOwnedBuilding(landlordId, form.BuildingId);
			RequireRoomTypeForProperty(form.RoomTypeId, building.PropertyId);
			EnsureFloor(form.Floor, building.NumberOfFloors);
			EnsureUniqueRoomNumber(form.BuildingId, form.RoomNumber, form.Id);

			int occupancy = CountOccupancy(room);
			string status = ResolveStatus(form.Status, occupancy > 0);
			string roomNumber = form.RoomNumber.Trim();
			var before = new Dictionary<string, string?>
			{
				[AuditField.RoomNumber] = room.RoomNumber,
				[AuditField.Floor] = room.Floor.ToString(),
				[AuditField.TypeId] = room.RoomTypeId.ToString(),
				[AuditField.Status] = RoomStatus.Format(room.Status)
			};
			var after = new Dictionary<string, string?>
			{
				[AuditField.RoomNumber] = roomNumber,
				[AuditField.Floor] = form.Floor.ToString(),
				[AuditField.TypeId] = form.RoomTypeId.ToString(),
				[AuditField.Status] = RoomStatus.Format(status)
			};
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			_rooms.Update(new Room
			{
				Id = form.Id,
				RoomTypeId = form.RoomTypeId,
				RoomNumber = roomNumber,
				Floor = form.Floor,
				Status = status,
				Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
			});

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Update,
				TableName = AuditObject.Rooms,
				RecordId = form.Id.ToString(),
				Detail = roomNumber,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Delete only when no contracts exist
		public void Delete(int landlordId, int roomId)
		{
			var room = RequireOwnedRoom(landlordId, roomId);
			if (_rooms.HasContracts(room))
			{
				throw new InvalidOperationException("Không thể xóa phòng đã có hợp đồng.");
			}
			if (!_rooms.Delete(room))
			{
				throw new InvalidOperationException("Không thể xóa phòng.");
			}

			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Delete,
				TableName = AuditObject.Rooms,
				RecordId = roomId.ToString(),
				Detail = room.RoomNumber,
				OldValue = AuditDiff.FormatOldOnly(new Dictionary<string, string?>
				{
					[AuditField.BuildingId] = room.BuildingId.ToString(),
					[AuditField.RoomNumber] = room.RoomNumber
				}),
				NewValue = null,
				Timestamp = DateTime.Now
			});
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

		// Guard room → building → property → landlord ownership chain
		private Room RequireOwnedRoom(int landlordId, int roomId)
		{
			var room = _rooms.GetById(roomId);
			if (room == null || room.Building?.Property == null || room.Building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Phòng không thuộc về bạn.");
			}
			return room;
		}

		// Room type must exist and belong to the given property
		private void RequireRoomTypeForProperty(int roomTypeId, int propertyId)
		{
			var roomType = _roomTypes.GetById(roomTypeId);
			if (roomType == null || roomType.PropertyId != propertyId)
			{
				throw new ArgumentException("Loại phòng không thuộc nhà trọ đã chọn.");
			}
		}

		// Room number must be unique within the building
		private void EnsureUniqueRoomNumber(int buildingId, string roomNumber, int? excludeRoomId)
		{
			if (_rooms.RoomNumberExists(buildingId, roomNumber, excludeRoomId))
			{
				throw new ArgumentException("Số phòng đã tồn tại trong tòa nhà này.");
			}
		}

		// Floor must be within 1..NumberOfFloors inclusive
		private static void EnsureFloor(int floor, int numberOfFloors)
		{
			if (floor < 1 || floor > numberOfFloors)
			{
				throw new ArgumentException($"Tầng phải từ 1 đến {numberOfFloors}.");
			}
		}

		// Validate room form fields
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

		// Normalize status string to canonical room status value
		private static string NormalizeStatus(string status)
		{
			return RoomStatus.Normalize(status);
		}

		// When occupants exist always Occupied; otherwise normalize from form
		private static string ResolveStatus(string requested, bool hasOccupants)
		{
			if (hasOccupants)
			{
				return RoomStatus.Occupied;
			}
			return NormalizeStatus(requested);
		}

		// Count distinct tenants on Active contracts for the room
		private static int CountOccupancy(Room r)
		{
			return r.Contracts?
				.Where(c => c.Status == ContractStatus.Active)
				.SelectMany(c => c.ContractTenants)
				.Select(ct => ct.TenantId)
				.Distinct()
				.Count() ?? 0;
		}

		// Map Room entity to grid list DTO with occupancy counts
		private static RoomDto MapList(Room r)
		{
			int occupancy = CountOccupancy(r);

			return new RoomDto
			{
				Id = r.Id,
				BuildingId = r.BuildingId,
				BuildingName = r.Building?.Name ?? string.Empty,
				RoomTypeId = r.RoomTypeId,
				RoomTypeName = r.RoomType?.TypeName ?? string.Empty,
				RoomNumber = r.RoomNumber,
				Floor = r.Floor,
				Status = r.Status,
				StatusDisplay = RoomStatus.Format(r.Status),
				CurrentOccupancy = occupancy,
				MaxOccupancy = r.RoomType?.MaxOccupancy ?? 0,
				Notes = r.Notes
			};
		}
	}
}
