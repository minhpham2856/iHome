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
	// CRUD services (Service) per property plus assign/bulk sync RoomServices — Metered/PerPerson/PerRoom
	public class LandlordServiceService
	{
		private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
		{
			CalculationMethod.Metered,
			CalculationMethod.PerPerson,
			CalculationMethod.PerRoom
		};

		private readonly ServiceRepository _services;
		private readonly RoomServiceRepository _roomServices;
		private readonly PropertyRepository _properties;
		private readonly BuildingRepository _buildings;
		private readonly AuditLogRepository _audits;

		public LandlordServiceService() : this(new IHomeDbContext()) { }

		public LandlordServiceService(IHomeDbContext context)
		{
			// Service catalog data access against shared DbContext
			_services = new ServiceRepository(context);
			// Room-service assignment sync
			_roomServices = new RoomServiceRepository(context);
			// Property ownership checks
			_properties = new PropertyRepository(context);
			// Building ownership checks
			_buildings = new BuildingRepository(context);
			// Audit trail for create/update/assign operations
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
			// Guard property ownership before querying buildings
			RequireOwnedProperty(landlordId, propertyId);
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

		// Calculation method combo for service form
		public List<CalculationMethodOptionDto> GetCalculationMethodOptions()
		{
			// Return fixed list of allowed billing methods
			return new()
			{
				new() { Value = CalculationMethod.PerRoom, DisplayName = CalculationMethod.PerRoom },
				new() { Value = CalculationMethod.PerPerson, DisplayName = CalculationMethod.PerPerson },
				new() { Value = CalculationMethod.Metered, DisplayName = CalculationMethod.Metered }
			};
		}

		// Service grid filtered by property
		public List<ServiceDto> GetByProperty(int landlordId, int propertyId)
		{
			// Guard property ownership before querying services
			RequireOwnedProperty(landlordId, propertyId);
			// Load services for property and map to list DTOs
			return _services.GetByProperty(propertyId).Select(Map).ToList();
		}

		// Edit form for a service
		public ServiceFormDto? GetForm(int landlordId, int serviceId)
		{
			// Verify service belongs to landlord via property chain
			var service = RequireOwnedService(landlordId, serviceId);
			// Project entity fields into form DTO for two-way binding
			return new ServiceFormDto
			{
				Id = service.Id,
				PropertyId = service.PropertyId,
				ServiceName = service.ServiceName,
				Unit = service.Unit,
				UnitPrice = service.UnitPrice,
				CalculationMethod = service.CalculationMethod,
				IsActive = service.IsActive
			};
		}

		// Create a new service
		public void Create(int landlordId, ServiceFormDto form)
		{
			// Validate required form fields
			Validate(form);
			// Verify property ownership
			RequireOwnedProperty(landlordId, form.PropertyId);

			// Build new Service entity from trimmed form values
			var entity = new Service
			{
				PropertyId = form.PropertyId,
				ServiceName = form.ServiceName.Trim(),
				Unit = form.Unit.Trim(),
				UnitPrice = form.UnitPrice,
				CalculationMethod = NormalizeMethod(form.CalculationMethod),
				IsActive = form.IsActive
			};
			// INSERT; throw if repository reports failure
			if (!_services.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm dịch vụ.");
			}

			// Record create audit with new-value snapshot
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Create,
				TableName = AuditObject.Services,
				RecordId = entity.Id.ToString(),
				Detail = entity.ServiceName,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.ServiceName] = entity.ServiceName,
					[AuditField.Price] = entity.UnitPrice.ToString("N0"),
					[AuditField.Method] = FormatMethod(entity.CalculationMethod),
					[AuditField.Active] = DisplayText.FormatActive(entity.IsActive)
				}),
				Timestamp = DateTime.Now
			});
		}

		// Update an existing service
		public void Update(int landlordId, ServiceFormDto form)
		{
			// Validate required form fields
			Validate(form);
			// Load existing service and verify ownership
			var existing = RequireOwnedService(landlordId, form.Id);
			// Verify property ownership for form property id
			RequireOwnedProperty(landlordId, form.PropertyId);

			// Normalize editable fields
			string serviceName = form.ServiceName.Trim();
			string method = NormalizeMethod(form.CalculationMethod);
			// Snapshot values before edit for audit diff
			var before = new Dictionary<string, string?>
			{
				[AuditField.ServiceName] = existing.ServiceName,
				[AuditField.Price] = existing.UnitPrice.ToString("N0"),
				[AuditField.Method] = FormatMethod(existing.CalculationMethod),
				[AuditField.Active] = DisplayText.FormatActive(existing.IsActive)
			};
			// Snapshot values after edit for audit diff
			var after = new Dictionary<string, string?>
			{
				[AuditField.ServiceName] = serviceName,
				[AuditField.Price] = form.UnitPrice.ToString("N0"),
				[AuditField.Method] = FormatMethod(method),
				[AuditField.Active] = DisplayText.FormatActive(form.IsActive)
			};
			// Build compact old/new diff strings
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			// Persist updated service row
			_services.Update(new Service
			{
				Id = form.Id,
				ServiceName = serviceName,
				Unit = form.Unit.Trim(),
				UnitPrice = form.UnitPrice,
				CalculationMethod = method,
				IsActive = form.IsActive
			});

			// Write update audit entry
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.Update,
				TableName = AuditObject.Services,
				RecordId = form.Id.ToString(),
				Detail = serviceName,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
		}

		// Summary of services assigned to each room in a building
		public List<RoomServiceSummaryDto> GetRoomsWithServices(int landlordId, int buildingId)
		{
			// Guard building ownership before querying room assignments
			RequireOwnedBuilding(landlordId, buildingId);
			// Load rooms with assignments and project summary DTOs
			return _roomServices.GetRoomsWithServicesByBuilding(buildingId)
				.Select(r =>
				{
					// Collect active assigned services sorted by name
					var active = r.RoomServices
						.Where(rs => rs.IsActive && rs.Service != null)
						.OrderBy(rs => rs.Service.ServiceName)
						.ToList();
					// Build per-room assignment summary row
					return new RoomServiceSummaryDto
					{
						RoomId = r.Id,
						RoomNumber = r.RoomNumber,
						Floor = r.Floor,
						AssignedCount = active.Count,
						AssignedServicesSummary = active.Count == 0
							? "Chưa gán"
							: string.Join(", ", active.Select(rs => rs.Service.ServiceName))
					};
				})
				.ToList();
		}

		// Room checkbox list for bulk assign — optional pre-select preferredRoomId
		public List<AssignCheckOptionDto> GetAssignRoomCheckOptions(int landlordId, int buildingId, int? preferredRoomId = null)
		{
			// Guard building ownership before querying rooms
			RequireOwnedBuilding(landlordId, buildingId);
			// Map rooms to checkbox options with optional default selection
			return _roomServices.GetRoomsWithServicesByBuilding(buildingId)
				.Select(r => new AssignCheckOptionDto
				{
					Id = r.Id,
					DisplayName = $"Tầng {r.Floor} - Phòng {r.RoomNumber}",
					IsSelected = preferredRoomId.HasValue && preferredRoomId.Value == r.Id
				})
				.ToList();
		}

		// Active service checkbox list for bulk assign
		public List<AssignCheckOptionDto> GetAssignServiceCheckOptions(int landlordId, int propertyId)
		{
			// Guard property ownership before querying services
			RequireOwnedProperty(landlordId, propertyId);
			// Map active services to unchecked checkbox options
			return _services.GetByProperty(propertyId)
				.Where(s => s.IsActive)
				.Select(s => new AssignCheckOptionDto
				{
					Id = s.Id,
					DisplayName = $"{s.ServiceName} - {s.UnitPrice:N0} đ/{s.Unit} ({FormatMethod(s.CalculationMethod)})",
					IsSelected = false
				})
				.ToList();
		}

		// Service checkbox list for one room — include inactive if currently assigned (to allow uncheck/remove)
		public List<AssignCheckOptionDto> GetServiceOptionsForRoom(int landlordId, int roomId)
		{
			// Load room with assignments and verify ownership
			var room = RequireOwnedRoom(landlordId, roomId);
			// Resolve property scope from room's building
			int propertyId = room.Building.PropertyId;
			// Build set of currently assigned active service ids
			var assigned = room.RoomServices
				.Where(rs => rs.IsActive)
				.Select(rs => rs.ServiceId)
				.ToHashSet();

			// Show active services plus inactive ones already assigned to this room
			return _services.GetByProperty(propertyId)
				.Where(s => s.IsActive || assigned.Contains(s.Id))
				.Select(s => new AssignCheckOptionDto
				{
					Id = s.Id,
					DisplayName = $"{s.ServiceName} - {s.UnitPrice:N0} đ/{s.Unit} ({FormatMethod(s.CalculationMethod)})",
					IsSelected = assigned.Contains(s.Id)
				})
				.ToList();
		}

		// Sync service assignments for one room — validate services belong to property (+ keep inactive already assigned)
		public void SyncRoomAssignments(int landlordId, int roomId, IReadOnlyCollection<int> selectedServiceIds)
		{
			// Load room with assignments and verify ownership
			var room = RequireOwnedRoom(landlordId, roomId);
			// Resolve property scope from room's building
			int propertyId = room.Building.PropertyId;

			// Start allowed set with active services on property
			var allowed = _services.GetByProperty(propertyId)
				.Where(s => s.IsActive)
				.Select(s => s.Id)
				.ToHashSet();
			// Also allow inactive services already assigned (so sync can remove them)
			foreach (int id in room.RoomServices.Where(rs => rs.IsActive).Select(rs => rs.ServiceId))
			{
				allowed.Add(id);
			}

			// Reject any selected service outside allowed set
			if (selectedServiceIds.Any(id => !allowed.Contains(id)))
			{
				throw new InvalidOperationException("Có dịch vụ không thuộc nhà trọ hoặc không hợp lệ.");
			}

			// Persist assignment sync via repository
			_roomServices.SyncRoomAssignments(roomId, propertyId, selectedServiceIds);

			// Build audit detail string for selected service ids
			string services = selectedServiceIds.Count == 0
				? "(không có)"
				: string.Join(",", selectedServiceIds);
			// Record assign-service audit entry
			_audits.Add(new AuditLog
			{
				UserId = landlordId,
				Action = AuditAction.AssignService,
				TableName = AuditObject.RoomServices,
				RecordId = roomId.ToString(),
				Detail = room.RoomNumber,
				OldValue = null,
				NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
				{
					[AuditField.Services] = services
				}),
				Timestamp = DateTime.Now
			});
		}

		// Bulk sync same serviceIds to many roomIds in one building — audit each room
		public void SyncAssignmentsForRooms(int landlordId, int buildingId, IReadOnlyCollection<int> roomIds, IReadOnlyCollection<int> serviceIds)
		{
			// Verify building ownership and load building for property id
			var building = RequireOwnedBuilding(landlordId, buildingId);
			int propertyId = building.PropertyId;

			// Build set of valid room ids in this building
			var roomsInBuilding = _roomServices.GetRoomsWithServicesByBuilding(buildingId)
				.Select(r => r.Id)
				.ToHashSet();
			// Reject empty or out-of-scope room id list
			if (roomIds.Count == 0 || roomIds.Any(id => !roomsInBuilding.Contains(id)))
			{
				throw new InvalidOperationException("Danh sách phòng không hợp lệ.");
			}

			// Build set of active service ids on property
			var allowedServices = _services.GetByProperty(propertyId)
				.Where(s => s.IsActive)
				.Select(s => s.Id)
				.ToHashSet();
			// Reject empty or out-of-scope service id list
			if (serviceIds.Count == 0 || serviceIds.Any(id => !allowedServices.Contains(id)))
			{
				throw new InvalidOperationException("Danh sách dịch vụ không hợp lệ.");
			}

			// Comma-separated service ids for audit detail
			string services = string.Join(",", serviceIds);
			// Apply sync and audit per selected room
			foreach (int roomId in roomIds)
			{
				// Reload room for audit detail (room number)
				var room = RequireOwnedRoom(landlordId, roomId);
				// Persist assignment sync for this room
				_roomServices.SyncRoomAssignments(roomId, propertyId, serviceIds);
				// Record assign-service audit entry
				_audits.Add(new AuditLog
				{
					UserId = landlordId,
					Action = AuditAction.AssignService,
					TableName = AuditObject.RoomServices,
					RecordId = roomId.ToString(),
					Detail = room.RoomNumber,
					OldValue = null,
					NewValue = AuditDiff.FormatNewOnly(new Dictionary<string, string?>
					{
						[AuditField.Services] = services
					}),
					Timestamp = DateTime.Now
				});
			}
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
			// Load room with service assignments and building navigation
			var room = _roomServices.GetRoomWithServices(roomId);
			// Reject missing room or property not owned by landlord
			if (room == null || room.Building?.Property == null || room.Building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Phòng không thuộc về bạn.");
			}
			// Return verified entity to caller
			return room;
		}

		// Guard service → property → landlord ownership chain
		private Service RequireOwnedService(int landlordId, int serviceId)
		{
			// Load service with navigation to property
			var service = _services.GetById(serviceId);
			// Reject missing service or property not owned by landlord
			if (service == null || service.Property == null || service.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Dịch vụ không thuộc về bạn.");
			}
			// Return verified entity to caller
			return service;
		}

		// Validate service form fields
		private static void Validate(ServiceFormDto form)
		{
			// Reject invalid property id
			if (form.PropertyId <= 0)
			{
				throw new ArgumentException("Nhà trọ không hợp lệ.");
			}
			// Reject empty service name
			if (string.IsNullOrWhiteSpace(form.ServiceName))
			{
				throw new ArgumentException("Tên dịch vụ không được để trống.");
			}
			// Reject empty unit
			if (string.IsNullOrWhiteSpace(form.Unit))
			{
				throw new ArgumentException("Đơn vị tính không được để trống.");
			}
			// Reject negative unit price
			if (form.UnitPrice < 0)
			{
				throw new ArgumentException("Đơn giá không được âm.");
			}
			// Reject unknown calculation method
			if (!AllowedMethods.Contains(form.CalculationMethod ?? string.Empty))
			{
				throw new ArgumentException("Cách tính phí không hợp lệ.");
			}
		}

		// Case-insensitive match → canonical PascalCase method string
		private static string NormalizeMethod(string method)
		{
			// Resolve to canonical allowed method value
			return AllowedMethods.First(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase));
		}

		// Map Service entity to list DTO with assignment count
		private static ServiceDto Map(Service s)
		{
			// Project entity fields and assignment count into list DTO
			return new ServiceDto
			{
				Id = s.Id,
				PropertyId = s.PropertyId,
				PropertyName = s.Property?.Name ?? string.Empty,
				ServiceName = s.ServiceName,
				Unit = s.Unit,
				UnitPrice = s.UnitPrice,
				CalculationMethod = s.CalculationMethod,
				CalculationMethodDisplay = FormatMethod(s.CalculationMethod),
				IsActive = s.IsActive,
				StatusDisplay = s.IsActive ? "Đang hoạt động" : "Ngừng hoạt động",
				AssignmentCount = s.RoomServices?.Count(rs => rs.IsActive) ?? 0
			};
		}

		// Format calculation method for display
		private static string FormatMethod(string method)
		{
			// Delegate to shared CalculationMethod helper
			return CalculationMethod.Format(method);
		}
	}
}
