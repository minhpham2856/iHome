using iHome.BLL.DTOs;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerRoomService
	{
		private const string ContractActive = "Active";
		private readonly ManagerReadRepository _repository = new();

		public List<ManagerRoomDto> GetRooms(int managerId, int? propertyId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetRooms(managerId, propertyId)
				.Select(r => new ManagerRoomDto
				{
					Id = r.Id,
					BuildingId = r.BuildingId,
					BuildingName = r.Building.Name,
					RoomNumber = r.RoomNumber,
					RoomTypeName = r.RoomType.TypeName,
					Floor = r.Floor,
					Area = r.Area,
					BaseRent = r.BaseRent,
					CurrentOccupancy = r.Contracts
						.Where(c => c.Status == ContractActive)
						.SelectMany(c => c.ContractTenants)
						.Select(ct => ct.TenantId)
						.Distinct()
						.Count(),
					MaxOccupancy = r.RoomType.MaxOccupancy,
					Status = r.Status,
					StatusDisplay = ManagerDisplayFormatter.FormatRoomStatus(r.Status),
					Notes = r.Notes
				})
				.ToList();
		}
	}
}
