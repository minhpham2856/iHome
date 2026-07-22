using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Danh sách phòng (chỉ đọc) — occupancy lấy từ hợp đồng đang hoạt động
	public class RoomService
	{
		private readonly ManagerReadRepository _repository = new();

		// Lưới phòng trong phạm vi quản lý (+ lọc tòa tùy chọn)
		public List<RoomDto> GetRooms(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetRooms(managerId, buildingId)
				.Select(r => new RoomDto
				{
					Id = r.Id,
					BuildingId = r.BuildingId,
					BuildingName = r.Building.Name,
					RoomNumber = r.RoomNumber,
					RoomTypeName = r.RoomType.TypeName,
					Floor = r.Floor,
					Area = r.RoomType.Area,
					BaseRent = r.RoomType.BaseRent,
					// Số khách trên HĐ đang hoạt động (không phải bắt buộc = MaxOccupancy)
					CurrentOccupancy = r.Contracts
						.Where(c => c.Status == ContractStatus.Active)
						.SelectMany(c => c.ContractTenants)
						.Select(ct => ct.TenantId)
						.Distinct()
						.Count(),
					MaxOccupancy = r.RoomType.MaxOccupancy,
					Status = r.Status,
					StatusDisplay = DisplayFormatter.FormatRoomStatus(r.Status),
					Notes = r.Notes
				})
				.ToList();
		}
	}
}
