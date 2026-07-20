using iHome.BLL.DTOs;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerServiceCatalogService
	{
		private readonly ManagerReadRepository _repository = new();

		public List<ManagerServiceDto> GetServices(int managerId, int? buildingId = null)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetServices(managerId, buildingId)
				.Select(s => new ManagerServiceDto
				{
					Id = s.Id,
					PropertyId = s.PropertyId,
					PropertyName = s.Property.Name,
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
