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
			// Room data access against shared DbContext
			_rooms = new RoomRepository(context);
			// Room type validation for property scope
			_roomTypes = new RoomTypeRepository(context);
			// Building ownership checks
			_buildings = new BuildingRepository(context);
			// Property filter options
			_properties = new PropertyRepository(context);
			// Audit trail for create/update/delete operations
			_audits = new AuditLogRepository(context);
		}

		// Active property combo for landlord filter
		public List<PropertyFilterOptionDto> GetPropertyOptions(int landlordId)
		{
			// Load landlord properties, keep active only, map to filter DTOs
			return _properties.GetByLandlord(landlordId)
				.Where(p => p.IsActive)
				.Select(p => new PropertyFilterOptionDto { Id = p.Id, Name = p.Name })
				.ToList();
		}

		// Active building combo filtered by property
		public List<BuildingFilterOptionDto> GetBuildingOptions(int landlordId, int propertyId)
		{
			// Load property and verify landlord ownership
			var property = _properties.GetById(propertyId);
			// Reject missing or foreign-owned properties
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}

			// Load active buildings for property and map to filter DTOs
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

		// Room type combo scoped to property
		public List<RoomTypeOptionDto> GetRoomTypeOptions(int landlordId, int propertyId)
		{
			// Load property and verify landlord ownership
			var property = _properties.GetById(propertyId);
			// Reject missing or foreign-owned properties
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}

			// Load room types for property and map to combo DTOs
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
			// Guard building ownership before querying rooms
			RequireOwnedBuilding(landlordId, buildingId);
			// Load rooms for building and map to list DTOs
			return _rooms.GetByBuilding(buildingId).Select(MapList).ToList();
		}

		// Create/edit form for a room
		public RoomFormDto? GetForm(int landlordId, int roomId)
		{
			// Verify room belongs to landlord via building/property chain
			var room = RequireOwnedRoom(landlordId, roomId);
			// Project entity fields into form DTO for two-way binding
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
			// Verify room belongs to landlord via building/property chain
			var room = RequireOwnedRoom(landlordId, roomId);
			// Collect distinct tenants from active contracts, main tenant first
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

			// Assemble detail DTO with room metadata and tenant list
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
			// Validate required form fields
			Validate(form);
			// Verify building ownership and load building for floor limit
			var building = RequireOwnedBuilding(landlordId, form.BuildingId);
			// Ensure room type belongs to building's property
			RequireRoomTypeForProperty(form.RoomTypeId, building.PropertyId);
			// Ensure floor is within building range
			EnsureFloor(form.Floor, building.NumberOfFloors);
			// Ensure room number is unique within building
			EnsureUniqueRoomNumber(form.BuildingId, form.RoomNumber, null);

			// Build new Room entity from trimmed form values
			var entity = new Room
			{
				BuildingId = form.BuildingId,
				RoomTypeId = form.RoomTypeId,
				RoomNumber = form.RoomNumber.Trim(),
				Floor = form.Floor,
				Status = NormalizeStatus(form.Status),
				Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
			};
			// INSERT; throw if repository reports failure
			if (!_rooms.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm phòng.");
			}

			// Record create audit with new-value snapshot
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
			// Validate required form fields
			Validate(form);
			// Load existing room and verify ownership
			var room = RequireOwnedRoom(landlordId, form.Id);
			// Verify building ownership and load building for floor limit
			var building = RequireOwnedBuilding(landlordId, form.BuildingId);
			// Ensure room type belongs to building's property
			RequireRoomTypeForProperty(form.RoomTypeId, building.PropertyId);
			// Ensure floor is within building range
			EnsureFloor(form.Floor, building.NumberOfFloors);
			// Ensure room number is unique within building (exclude self)
			EnsureUniqueRoomNumber(form.BuildingId, form.RoomNumber, form.Id);

			// Count current occupants from active contracts
			int occupancy = CountOccupancy(room);
			// Resolve status — occupied rooms cannot be set vacant via form
			string status = ResolveStatus(form.Status, occupancy > 0);
			// Normalize room number
			string roomNumber = form.RoomNumber.Trim();
			// Snapshot values before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.RoomNumber] = room.RoomNumber,
				[AuditField.Floor] = room.Floor.ToString(),
				[AuditField.TypeId] = room.RoomTypeId.ToString(),
				[AuditField.Status] = RoomStatus.Format(room.Status)
			};
			// Snapshot values after edit for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.RoomNumber] = roomNumber,
				[AuditField.Floor] = form.Floor.ToString(),
				[AuditField.TypeId] = form.RoomTypeId.ToString(),
				[AuditField.Status] = RoomStatus.Format(status)
			};
			// Build compact old/new diff strings
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			// Persist updated room row
			_rooms.Update(new Room
			{
				Id = form.Id,
				RoomTypeId = form.RoomTypeId,
				RoomNumber = roomNumber,
				Floor = form.Floor,
				Status = status,
				Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim()
			});

			// Write update audit entry
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
			// Load room and verify ownership
			var room = RequireOwnedRoom(landlordId, roomId);
			// Block delete when contracts reference this room
			if (_rooms.HasContracts(room))
			{
				throw new InvalidOperationException("Không thể xóa phòng đã có hợp đồng.");
			}
			// DELETE; throw if repository reports failure
			if (!_rooms.Delete(room))
			{
				throw new InvalidOperationException("Không thể xóa phòng.");
			}

			// Record delete audit with old-value snapshot
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
			// Load building with navigation to property
			var building = _buildings.GetById(buildingId);
			// Reject missing building or property not owned by landlord
			if (building == null || building.Property == null || building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Tòa nhà không thuộc về bạn.");
			}
			// Return verified entity to caller
			return building;
		}

		// Guard room → building → property → landlord ownership chain
		private Room RequireOwnedRoom(int landlordId, int roomId)
		{
			// Load room with navigation to building and property
			var room = _rooms.GetById(roomId);
			// Reject missing room or property not owned by landlord
			if (room == null || room.Building?.Property == null || room.Building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Phòng không thuộc về bạn.");
			}
			// Return verified entity to caller
			return room;
		}

		// Room type must exist and belong to the given property
		private void RequireRoomTypeForProperty(int roomTypeId, int propertyId)
		{
			// Load room type by primary key
			var roomType = _roomTypes.GetById(roomTypeId);
			// Reject missing type or wrong property scope
			if (roomType == null || roomType.PropertyId != propertyId)
			{
				throw new ArgumentException("Loại phòng không thuộc nhà trọ đã chọn.");
			}
		}

		// Room number must be unique within the building
		private void EnsureUniqueRoomNumber(int buildingId, string roomNumber, int? excludeRoomId)
		{
			// Check repository for duplicate room number
			if (_rooms.RoomNumberExists(buildingId, roomNumber, excludeRoomId))
			{
				throw new ArgumentException("Số phòng đã tồn tại trong tòa nhà này.");
			}
		}

		// Floor must be within 1..NumberOfFloors inclusive
		private static void EnsureFloor(int floor, int numberOfFloors)
		{
			// Reject floor outside building range
			if (floor < 1 || floor > numberOfFloors)
			{
				throw new ArgumentException($"Tầng phải từ 1 đến {numberOfFloors}.");
			}
		}

		// Validate room form fields
		private static void Validate(RoomFormDto form)
		{
			// Reject missing building selection
			if (form.BuildingId <= 0)
			{
				throw new ArgumentException("Chưa chọn tòa nhà.");
			}
			// Reject missing room type selection
			if (form.RoomTypeId <= 0)
			{
				throw new ArgumentException("Chưa chọn loại phòng.");
			}
			// Reject empty room number
			if (string.IsNullOrWhiteSpace(form.RoomNumber))
			{
				throw new ArgumentException("Số phòng không được để trống.");
			}
			// Reject invalid floor
			if (form.Floor < 1)
			{
				throw new ArgumentException("Tầng phải lớn hơn hoặc bằng 1.");
			}
		}

		// Normalize status string to canonical room status value
		private static string NormalizeStatus(string status)
		{
			// Delegate to shared RoomStatus helper
			return RoomStatus.Normalize(status);
		}

		// When occupants exist always Occupied; otherwise normalize from form
		private static string ResolveStatus(string requested, bool hasOccupants)
		{
			// Active tenants force occupied status
			if (hasOccupants)
			{
				return RoomStatus.Occupied;
			}
			// No occupants — use normalized form status
			return NormalizeStatus(requested);
		}

		// Count distinct tenants on Active contracts for the room
		private static int CountOccupancy(Room r)
		{
			// Sum distinct tenant ids across active contract links
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
			// Compute current occupancy from active contracts
			int occupancy = CountOccupancy(r);

			// Project entity fields and occupancy into list DTO
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
				StatusDisplay = RoomStatus.Format(r.Status),
				CurrentOccupancy = occupancy,
				MaxOccupancy = r.RoomType?.MaxOccupancy ?? 0,
				Notes = r.Notes
			};
		}
	}
}
