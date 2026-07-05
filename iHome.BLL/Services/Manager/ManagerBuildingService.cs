using iHome.BLL.DTOs;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerBuildingService
	{
		private readonly ManagerReadRepository _repository = new();

		public List<ManagerPropertyOptionDto> GetProperties(int managerId)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetProperties(managerId)
				.Select(p => new ManagerPropertyOptionDto
				{
					Id = p.Id,
					Name = p.Name,
					Address = p.Address
				})
				.ToList();
		}

		public List<ManagerBuildingOptionDto> GetBuildings(
			int managerId,
			int? propertyId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetBuildings(managerId, propertyId)
				.Select(b => new ManagerBuildingOptionDto
				{
					Id = b.Id,
					Name = b.Name
				})
				.ToList();
		}
	}
}
