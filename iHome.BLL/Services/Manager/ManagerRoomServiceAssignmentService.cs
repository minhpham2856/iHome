using iHome.BLL.DTOs;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerRoomServiceAssignmentService
	{
		private readonly ManagerOperationsRepository _operationsRepository = new();
		private readonly ManagerReadRepository _readRepository = new();

		public List<ManagerRoomServiceAssignmentDto> GetAssignments(int managerId, int? buildingId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			return _operationsRepository.GetRoomServices(managerId, buildingId)
				.Select(item => new ManagerRoomServiceAssignmentDto
				{
					RoomId = item.RoomId,
					ServiceId = item.ServiceId,
					BuildingName = item.Room.Building.Name,
					RoomNumber = item.Room.RoomNumber,
					ServiceName = item.Service.ServiceName,
					Unit = item.Service.Unit,
					UnitPrice = item.Service.UnitPrice,
					IsActive = item.IsActive
				})
				.ToList();
		}

		public List<ManagerLookupOptionDto> GetRoomOptions(int managerId, int? buildingId = null) =>
			_readRepository.GetRooms(managerId, buildingId)
				.Select(room => new ManagerLookupOptionDto
				{
					Id = room.Id,
					BuildingId = room.BuildingId,
					PropertyId = room.Building.PropertyId,
					DisplayName = $"{room.Building.Name} - Phòng {room.RoomNumber}"
				})
				.ToList();

		public List<ManagerLookupOptionDto> GetServiceOptions(int managerId, int? buildingId = null) =>
			_readRepository.GetServices(managerId, buildingId)
				.Where(service => service.IsActive)
				.Select(service => new ManagerLookupOptionDto
				{
					Id = service.Id,
					PropertyId = service.PropertyId,
					DisplayName = $"{service.ServiceName} - {service.UnitPrice:N0} đ/{service.Unit}"
				})
				.ToList();

		public void SetAssignment(int managerId, int roomId, int serviceId, bool isActive)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.SetRoomService(managerId, roomId, serviceId, isActive);
		}
	}
}
