using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Read-only room listing for Manager — occupancy derived from active contracts
	public class RoomService
	{
		// Read repository for rooms in manager-assigned buildings
		private readonly ManagerReadRepository _repository = new();

		// Grid rows: room metadata + live occupancy count from active contracts
		public List<RoomDto> GetRooms(int managerId, int? buildingId = null)
		{
			// Reject invalid manager id before scoped query
			ServiceGuard.EnsureValidManagerId(managerId);

			// Load rooms in buildings assigned to this manager
			return _repository.GetRooms(managerId, buildingId)
				.Select(r => new RoomDto
				{
					// Room primary key
					Id = r.Id,
					// Parent building id for filters
					BuildingId = r.BuildingId,
					// Building name for grid column
					BuildingName = r.Building.Name,
					// Room number label within building
					RoomNumber = r.RoomNumber,
					// Room type name from RoomType navigation
					RoomTypeName = r.RoomType.TypeName,
					// Floor index within building
					Floor = r.Floor,
					// Area copied from room type template
					Area = r.RoomType.Area,
					// Base rent from room type — may differ from contract MonthlyRent
					BaseRent = r.RoomType.BaseRent,
					// Count distinct tenants on Active contracts for this room
					CurrentOccupancy = r.Contracts
						.Where(c => c.Status == ContractStatus.Active)
						.SelectMany(c => c.ContractTenants)
						.Select(ct => ct.TenantId)
						.Distinct()
						.Count(),
					// Maximum allowed occupants from room type
					MaxOccupancy = r.RoomType.MaxOccupancy,
					// Raw status string stored in DB
					Status = r.Status,
					// Vietnamese status for UI display
					StatusDisplay = DisplayFormatter.FormatRoomStatus(r.Status),
					// Optional free-text notes from landlord/manager
					Notes = r.Notes
				})
				.ToList();
		}
	}
}
