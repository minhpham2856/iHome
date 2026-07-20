using iHome.BLL.DTOs.Landlord;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services
{
	public class LandlordServiceService
	{
		private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
		{
			"Metered", "PerPerson", "PerRoom"
		};

		private readonly ServiceRepository _services;
		private readonly RoomServiceRepository _roomServices;
		private readonly PropertyRepository _properties;
		private readonly BuildingRepository _buildings;
		private readonly AuditLogRepository _audits;

		public LandlordServiceService() : this(new IHomeDbContext())
		{
		}

		public LandlordServiceService(IHomeDbContext context)
		{
			_services = new ServiceRepository(context);
			_roomServices = new RoomServiceRepository(context);
			_properties = new PropertyRepository(context);
			_buildings = new BuildingRepository(context);
			_audits = new AuditLogRepository(context);
		}

		public List<PropertyFilterOptionDto> GetPropertyOptions(int landlordId) =>
			_properties.GetByLandlord(landlordId)
				.Where(p => p.IsActive)
				.Select(p => new PropertyFilterOptionDto { Id = p.Id, Name = p.Name })
				.ToList();

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

		public List<CalculationMethodOptionDto> GetCalculationMethodOptions() =>
			new()
			{
				new() { Value = "PerRoom", DisplayName = "Theo phòng" },
				new() { Value = "PerPerson", DisplayName = "Theo người" },
				new() { Value = "Metered", DisplayName = "Chỉ số (theo phòng)" }
			};

		public List<ServiceDto> GetByProperty(int landlordId, int propertyId)
		{
			RequireOwnedProperty(landlordId, propertyId);
			return _services.GetByProperty(propertyId).Select(Map).ToList();
		}

		public List<ServiceDto> GetActiveByProperty(int landlordId, int propertyId) =>
			GetByProperty(landlordId, propertyId).Where(s => s.IsActive).ToList();

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

			_audits.Add(
				landlordId,
				"Create",
				"Services",
				entity.Id.ToString(),
				null,
				$"Name={entity.ServiceName}; Price={entity.UnitPrice}; Method={entity.CalculationMethod}; Active={entity.IsActive}");
		}

		public void Update(int landlordId, ServiceFormDto form)
		{
			Validate(form);
			var existing = RequireOwnedService(landlordId, form.Id);
			RequireOwnedProperty(landlordId, form.PropertyId);

			string oldValue =
				$"Name={existing.ServiceName}; Price={existing.UnitPrice}; Method={existing.CalculationMethod}; Active={existing.IsActive}";
			string serviceName = form.ServiceName.Trim();
			string method = NormalizeMethod(form.CalculationMethod);

			_services.Update(new Service
			{
				Id = form.Id,
				ServiceName = serviceName,
				Unit = form.Unit.Trim(),
				UnitPrice = form.UnitPrice,
				CalculationMethod = method,
				IsActive = form.IsActive
			});

			_audits.Add(
				landlordId,
				"Update",
				"Services",
				form.Id.ToString(),
				oldValue,
				$"Name={serviceName}; Price={form.UnitPrice}; Method={method}; Active={form.IsActive}");
		}

		public void Deactivate(int landlordId, int serviceId)
		{
			var existing = RequireOwnedService(landlordId, serviceId);
			if (!_services.Disable(serviceId))
			{
				throw new InvalidOperationException("Không thể ngừng dịch vụ.");
			}

			_audits.Add(
				landlordId,
				"Disable",
				"Services",
				serviceId.ToString(),
				$"Name={existing.ServiceName}; Active=true",
				"Active=false");
		}

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

			string services = selectedServiceIds.Count == 0
				? "(none)"
				: string.Join(",", selectedServiceIds);
			_audits.Add(
				landlordId,
				"AssignService",
				"RoomServices",
				roomId.ToString(),
				null,
				$"Services={services}");
		}

		// gán bộ dịch vụ đã chọn cho từng phòng đã chọn (thay thế danh sách gán của phòng đó)
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

			string services = string.Join(",", serviceIds);
			foreach (int roomId in roomIds)
			{
				_roomServices.SyncRoomAssignments(roomId, propertyId, serviceIds);
				_audits.Add(
					landlordId,
					"AssignService",
					"RoomServices",
					roomId.ToString(),
					null,
					$"Services={services}");
			}
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
			var room = _roomServices.GetRoomWithServices(roomId);
			if (room == null || room.Building?.Property == null || room.Building.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Phòng không thuộc về bạn.");
			}
			return room;
		}

		private Service RequireOwnedService(int landlordId, int serviceId)
		{
			var service = _services.GetById(serviceId);
			if (service == null || service.Property == null || service.Property.LandlordId != landlordId)
			{
				throw new UnauthorizedAccessException("Dịch vụ không thuộc về bạn.");
			}
			return service;
		}

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

		private static string NormalizeMethod(string method) =>
			AllowedMethods.First(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase));

		private static ServiceDto Map(Service s) =>
			new()
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

		private static string FormatMethod(string method) => method switch
		{
			"Metered" => "Chỉ số (theo phòng)",
			"PerPerson" => "Theo người",
			"PerRoom" => "Theo phòng",
			"Fixed" => "Theo phòng",
			_ => method
		};
	}
}
