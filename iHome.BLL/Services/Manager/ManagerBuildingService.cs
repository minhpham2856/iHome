using iHome.BLL.DTOs;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerBuildingService
	{
		private readonly ManagerReadRepository _repository = new();

		public List<ManagerBuildingOptionDto> GetAssignedBuildings(int managerId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetBuildings(managerId)
				.Select(b => new ManagerBuildingOptionDto
				{
					Id = b.Id,
					Name = b.Name
				})
				.ToList();
		}
	}
}
