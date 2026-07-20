using iHome.BLL.DTOs;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerServiceCatalogService
	{
		private readonly ManagerReadRepository _repository = new();

		public List<ManagerServiceDto> GetServices(int managerId, int? propertyId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetServices(managerId, propertyId)
				.Select(s => new ManagerServiceDto
				{
					Id = s.Id,
					BuildingId = s.BuildingId,
					BuildingName = s.Building.Name,
					ServiceName = s.ServiceName,
					Unit = s.Unit,
					UnitPrice = s.UnitPrice,
					CalculationMethod = s.CalculationMethod,
					CalculationMethodDisplay = ManagerDisplayFormatter.FormatCalculationMethod(s.CalculationMethod),
					IsActive = s.IsActive,
					StatusDisplay = s.IsActive ? "Đang hoạt động" : "Ngừng hoạt động"
				})
				.ToList();
		}
	}
}
