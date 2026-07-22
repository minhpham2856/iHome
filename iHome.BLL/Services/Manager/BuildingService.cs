using iHome.BLL.DTOs.Manager;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Tòa nhà được gán cho quản lý — dùng cho combo lọc
	public class BuildingService
	{
		private readonly ManagerReadRepository _repository = new();

		// Id/Name các tòa ManagerId = managerId
		public List<BuildingOptionDto> GetAssignedBuildings(int managerId)
		{
			ServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetBuildings(managerId)
				.Select(b => new BuildingOptionDto
				{
					Id = b.Id,
					Name = b.Name
				})
				.ToList();
		}
	}
}
