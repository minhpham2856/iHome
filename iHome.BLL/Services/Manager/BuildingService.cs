using iHome.BLL.DTOs.Manager;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Load buildings assigned to the signed-in Manager for filter/navigation combos
	public class BuildingService
	{
		// Read-only repository scoped to manager's assigned buildings
		private readonly ManagerReadRepository _repository = new();

		// Return Id/Name pairs for buildings this manager may operate on
		public List<BuildingOptionDto> GetAssignedBuildings(int managerId)
		{
			// Validate managerId before hitting DAL — prevents empty or cross-tenant queries
			ServiceGuard.EnsureValidManagerId(managerId);

			// Query buildings where Building.ManagerId = managerId, map to lightweight combo DTO
			return _repository.GetBuildings(managerId)
				.Select(b => new BuildingOptionDto
				{
					// Primary key for building filter selection
					Id = b.Id,
					// Display label in building dropdown
					Name = b.Name
				})
				.ToList();
		}
	}
}
