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
			_services = new ServiceRepository(context);
			_roomServices = new RoomServiceRepository(context);
			_properties = new PropertyRepository(context);
			_buildings = new BuildingRepository(context);
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
			RequireOwnedProperty(landlordId, propertyId);
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
			RequireOwnedProperty(landlordId, propertyId);
			return _services.GetByProperty(propertyId).Select(Map).ToList();
		}

		// Edit form for a service
		public ServiceFormDto? GetForm(int landlordId, int serviceId)
		{
			var service = RequireOwnedService(landlordId, serviceId);
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
			Validate(form);
			RequireOwnedProperty(landlordId, form.PropertyId);

			var entity = new Service
			{
				PropertyId = form.PropertyId,
				ServiceName = form.ServiceName.Trim(),
				Unit = form.Unit.Trim(),
				UnitPrice = form.UnitPrice,
				CalculationMethod = NormalizeMethod(form.CalculationMethod),
				IsActive = form.IsActive
			};
			if (!_services.Add(entity))
			{
				throw new InvalidOperationException("Không thể thêm dịch vụ.");
			}

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
			Validate(form);
			var existing = RequireOwnedService(landlordId, form.Id);
			RequireOwnedProperty(landlordId, form.PropertyId);

			string serviceName = form.ServiceName.Trim();
			string method = NormalizeMethod(form.CalculationMethod);
			var before = new Dictionary<string, string?>
			{
				[AuditField.ServiceName] = existing.ServiceName,
				[AuditField.Price] = existing.UnitPrice.ToString("N0"),
				[AuditField.Method] = FormatMethod(existing.CalculationMethod),
				[AuditField.Active] = DisplayText.FormatActive(existing.IsActive)
			};
			var after = new Dictionary<string, string?>
			{
				[AuditField.ServiceName] = serviceName,
				[AuditField.Price] = form.UnitPrice.ToString("N0"),
				[AuditField.Method] = FormatMethod(method),
				[AuditField.Active] = DisplayText.FormatActive(form.IsActive)
			};
			var (oldValue, newValue) = AuditDiff.Build(before, after);

			_services.Update(new Service
			{
				Id = form.Id,
				ServiceName = serviceName,
				Unit = form.Unit.Trim(),
				UnitPrice = form.UnitPrice,
				CalculationMethod = method,
				IsActive = form.IsActive
			});

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
			RequireOwnedBuilding(landlordId, buildingId);
			return _roomServices.GetRoomsWithServicesByBuilding(buildingId)
				.Select(r =>
				{
					var active = r.RoomServices
						.Where(rs => rs.IsActive && rs.Service != null)
						.OrderBy(rs => rs.Service.ServiceName)
						.ToList();
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
			RequireOwnedBuilding(landlordId, buildingId);
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
			RequireOwnedProperty(landlordId, propertyId);
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
			var room = RequireOwnedRoom(landlordId, roomId);
			int propertyId = room.Building.PropertyId;
			var assigned = room.RoomServices
				.Where(rs => rs.IsActive)
				.Select(rs => rs.ServiceId)
				.ToHashSet();

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
			var room = RequireOwnedRoom(landlordId, roomId);
			int propertyId = room.Building.PropertyId;

			var allowed = _services.GetByProperty(propertyId)
				.Where(s => s.IsActive)
				.Select(s => s.Id)
				.ToHashSet();
			foreach (int id in room.RoomServices.Where(rs => rs.IsActive).Select(rs => rs.ServiceId))
			{
				allowed.Add(id);
			}

			if (selectedServiceIds.Any(id => !allowed.Contains(id)))
			{
				throw new InvalidOperationException("Có dịch vụ không thuộc nhà trọ hoặc không hợp lệ.");
			}

			_roomServices.SyncRoomAssignments(roomId, propertyId, selectedServiceIds);

			string services = FormatServiceNames(propertyId, selectedServiceIds);
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
			var building = RequireOwnedBuilding(landlordId, buildingId);
			int propertyId = building.PropertyId;

			var roomsInBuilding = _roomServices.GetRoomsWithServicesByBuilding(buildingId)
				.Select(r => r.Id)
				.ToHashSet();
			if (roomIds.Count == 0 || roomIds.Any(id => !roomsInBuilding.Contains(id)))
			{
				throw new InvalidOperationException("Danh sách phòng không hợp lệ.");
			}

			var allowedServices = _services.GetByProperty(propertyId)
				.Where(s => s.IsActive)
				.Select(s => s.Id)
				.ToHashSet();
			if (serviceIds.Count == 0 || serviceIds.Any(id => !allowedServices.Contains(id)))
			{
				throw new InvalidOperationException("Danh sách dịch vụ không hợp lệ.");
			}

			string services = FormatServiceNames(propertyId, serviceIds);
			foreach (int roomId in roomIds)
			{
				var room = RequireOwnedRoom(landlordId, roomId);
				_roomServices.SyncRoomAssignments(roomId, propertyId, serviceIds);
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
			var property = _properties.GetById(propertyId);
			if (property == null || property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
			return property;
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

		// Guard room → building → property → landlord ownership chain
		private Room RequireOwnedRoom(int landlordId, int roomId)
		{
			var room = _roomServices.GetRoomWithServices(roomId);
			if (room == null || room.Building?.Property == null || room.Building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Phòng không thuộc về bạn.");
			}
			return room;
		}

		// Guard service → property → landlord ownership chain
		private Service RequireOwnedService(int landlordId, int serviceId)
		{
			var service = _services.GetById(serviceId);
			if (service == null || service.Property == null || service.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Dịch vụ không thuộc về bạn.");
			}
			return service;
		}

		// Validate service form fields
		private static void Validate(ServiceFormDto form)
		{
			if (form.PropertyId <= 0)
			{
				throw new ArgumentException("Nhà trọ không hợp lệ.");
			}
			if (string.IsNullOrWhiteSpace(form.ServiceName))
			{
				throw new ArgumentException("Tên dịch vụ không được để trống.");
			}
			if (string.IsNullOrWhiteSpace(form.Unit))
			{
				throw new ArgumentException("Đơn vị tính không được để trống.");
			}
			if (form.UnitPrice < 0)
			{
				throw new ArgumentException("Đơn giá không được âm.");
			}
			if (!AllowedMethods.Contains(form.CalculationMethod ?? string.Empty))
			{
				throw new ArgumentException("Cách tính phí không hợp lệ.");
			}
		}

		// Case-insensitive match → canonical PascalCase method string
		private static string NormalizeMethod(string method)
		{
			return AllowedMethods.First(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase));
		}

		// Map Service entity to list DTO with assignment count
		private static ServiceDto Map(Service s)
		{
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

		// Resolve service ids to display names for audit logs
		private string FormatServiceNames(int propertyId, IReadOnlyCollection<int> serviceIds)
		{
			if (serviceIds.Count == 0) return "(không có)";
			var namesById = _services.GetByProperty(propertyId)
				.ToDictionary(s => s.Id, s => s.ServiceName);
			return string.Join(", ", serviceIds.Select(id =>
				namesById.TryGetValue(id, out string? name) ? name : id.ToString()));
		}

		// Format calculation method for display
		private static string FormatMethod(string method)
		{
			return CalculationMethod.Format(method);
		}
	}
}
