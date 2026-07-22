using iHome.BLL.DTOs.Manager;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Catalog dịch vụ (chỉ đọc) theo property trong phạm vi quản lý
	public class ServiceCatalogService
	{
		private readonly ManagerReadRepository _repository = new();

		// Danh sách dịch vụ cho lưới Manager
		public List<ServiceDto> GetServices(int managerId, int? buildingId = null)
		{
			ServiceGuard.EnsureValidManagerId(managerId);

			return _repository.GetServices(managerId, buildingId)
				.Select(s => new ServiceDto
				{
					Id = s.Id,
					PropertyId = s.PropertyId,
					PropertyName = s.Property.Name,
					ServiceName = s.ServiceName,
					Unit = s.Unit,
					UnitPrice = s.UnitPrice,
					CalculationMethod = s.CalculationMethod,
					CalculationMethodDisplay = DisplayFormatter.FormatCalculationMethod(s.CalculationMethod),
					IsActive = s.IsActive,
					StatusDisplay = s.IsActive ? "Đang hoạt động" : "Ngừng hoạt động"
				})
				.ToList();
		}
	}
}
