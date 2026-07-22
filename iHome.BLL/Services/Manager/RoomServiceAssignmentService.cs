using iHome.BLL.DTOs.Manager;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Manager view of room–service assignments; toggle active flag per pair
	public class RoomServiceAssignmentService
	{
		// Write repository for SetRoomService mutations
		private readonly ManagerOperationsRepository _operationsRepository = new();
		// Read repository for combo/lookup options
		private readonly ManagerReadRepository _readRepository = new();

		// Grid of room–service links in manager's scope
		public List<RoomServiceAssignmentDto> GetAssignments(int managerId, int? buildingId = null)
		{
			// Validate caller is a real manager user
			ServiceGuard.EnsureValidManagerId(managerId);
			// Load RoomServices rows filtered by manager buildings
			return _operationsRepository.GetRoomServices(managerId, buildingId)
				.Select(item => new RoomServiceAssignmentDto
				{
					// Composite key part: room id
					RoomId = item.RoomId,
					// Composite key part: service id
					ServiceId = item.ServiceId,
					// Building name for grid grouping
					BuildingName = item.Room.Building.Name,
					// Room number within building
					RoomNumber = item.Room.RoomNumber,
					// Service label (electricity, water, …)
					ServiceName = item.Service.ServiceName,
					// Billing unit from service definition
					Unit = item.Service.Unit,
					// Unit price at time of assignment (from Service)
					UnitPrice = item.Service.UnitPrice,
					// Whether this room currently bills for this service
					IsActive = item.IsActive
				})
				.ToList();
		}

		// Combo options: rooms the manager can assign services to
		public List<LookupOptionDto> GetRoomOptions(int managerId, int? buildingId = null) =>
			// No separate guard here — GetRooms internally validates via repository scope
			_readRepository.GetRooms(managerId, buildingId)
				.Select(room => new LookupOptionDto
				{
					// Room id for assignment target
					Id = room.Id,
					// Parent building — used when filtering services by property
					BuildingId = room.BuildingId,
					// Property id from building navigation
					PropertyId = room.Building.PropertyId,
					// Label shown in room picker: "Building - Phòng N"
					DisplayName = $"{room.Building.Name} - Phòng {room.RoomNumber}"
				})
				.ToList();

		// Combo options: active services available for assignment
		public List<LookupOptionDto> GetServiceOptions(int managerId, int? buildingId = null) =>
			_readRepository.GetServices(managerId, buildingId)
				// Only billable services — inactive catalog entries hidden from assign UI
				.Where(service => service.IsActive)
				.Select(service => new LookupOptionDto
				{
					// Service id for assignment
					Id = service.Id,
					// Property owning this service catalog
					PropertyId = service.PropertyId,
					// Label: name + formatted price per unit
					DisplayName = $"{service.ServiceName} - {service.UnitPrice:N0} đ/{service.Unit}"
				})
				.ToList();

		// Activate or deactivate a single room–service link
		public void SetAssignment(int managerId, int roomId, int serviceId, bool isActive)
		{
			// Guard manager id before mutating RoomServices
			ServiceGuard.EnsureValidManagerId(managerId);
			// DAL verifies room/service belong to manager's buildings before UPDATE/INSERT
			_operationsRepository.SetRoomService(managerId, roomId, serviceId, isActive);
		}
	}
}
