using iHome.BLL.DTOs.Manager;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Gán dịch vụ cho phòng — bật/tắt IsActive theo cặp phòng–dịch vụ
	public class RoomServiceAssignmentService
	{
		private readonly ManagerOperationsRepository _operationsRepository = new();
		private readonly ManagerReadRepository _readRepository = new();

		// Lưới gán dịch vụ trong phạm vi quản lý
		public List<RoomServiceAssignmentDto> GetAssignments(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			return _operationsRepository.GetRoomServices(managerId, buildingId)
				.Select(item => new RoomServiceAssignmentDto
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

		// Combo phòng để gán dịch vụ
		public List<LookupOptionDto> GetRoomOptions(int managerId, int? buildingId = null) =>
			_readRepository.GetRooms(managerId, buildingId)
				.Select(room => new LookupOptionDto
				{
					Id = room.Id,
					BuildingId = room.BuildingId,
					PropertyId = room.Building.PropertyId,
					DisplayName = $"{room.Building.Name} - Phòng {room.RoomNumber}"
				})
				.ToList();

		// Combo dịch vụ đang hoạt động
		public List<LookupOptionDto> GetServiceOptions(int managerId, int? buildingId = null) =>
			_readRepository.GetServices(managerId, buildingId)
				.Where(service => service.IsActive)
				.Select(service => new LookupOptionDto
				{
					Id = service.Id,
					PropertyId = service.PropertyId,
					DisplayName = $"{service.ServiceName} - {service.UnitPrice:N0} đ/{service.Unit}"
				})
				.ToList();

		// Bật/tắt một cặp phòng–dịch vụ (DAL kiểm tra phạm vi)
		public void SetAssignment(int managerId, int roomId, int serviceId, bool isActive)
		{
			ServiceGuard.EnsureValidManagerId(managerId);
			_operationsRepository.SetRoomService(managerId, roomId, serviceId, isActive);
		}
	}
}
