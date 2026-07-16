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

		public List<ManagerRoomServiceAssignmentDto> GetAssignments(int managerId, int? propertyId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);
			return _operationsRepository.GetRoomServices(managerId, propertyId)
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

		public List<ManagerLookupOptionDto> GetRoomOptions(int managerId, int? propertyId = null) =>
			_readRepository.GetRooms(managerId, propertyId)
				.Select(room => new ManagerLookupOptionDto
				{
					Id = room.Id,
					BuildingId = room.BuildingId,
					DisplayName = $"{room.Building.Name} - Phòng {room.RoomNumber}"
				})
				.ToList();

		public List<ManagerLookupOptionDto> GetServiceOptions(int managerId, int? propertyId = null) =>
			_readRepository.GetServices(managerId, propertyId)
				.Where(service => service.IsActive)
				.Select(service => new ManagerLookupOptionDto
				{
					Id = service.Id,
					BuildingId = service.BuildingId,
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
