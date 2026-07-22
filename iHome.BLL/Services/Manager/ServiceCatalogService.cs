using iHome.BLL.DTOs.Manager;
using iHome.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Read-only catalog of property services visible to the assigned Manager
	public class ServiceCatalogService
	{
		// Shared read repository for manager-scoped queries
		private readonly ManagerReadRepository _repository = new();

		// List active/inactive services for properties the manager can access
		public List<ServiceDto> GetServices(int managerId, int? buildingId = null)
		{
			// Guard manager id — all Manager BLL entry points call this first
			ServiceGuard.EnsureValidManagerId(managerId);

			// Load services filtered by manager assignment and optional building scope
			return _repository.GetServices(managerId, buildingId)
				.Select(s => new ServiceDto
				{
					// Service primary key
					Id = s.Id,
					// Owning property id — used when filtering by property
					PropertyId = s.PropertyId,
					// Property name for grid column
					PropertyName = s.Property.Name,
					// Vietnamese service label (electricity, water, etc.)
					ServiceName = s.ServiceName,
					// Billing unit (kWh, m³, tháng, …)
					Unit = s.Unit,
					// Price per unit before quantity multiplier
					UnitPrice = s.UnitPrice,
					// Raw calculation method stored in DB
					CalculationMethod = s.CalculationMethod,
					// Human-readable method for UI grid
					CalculationMethodDisplay = DisplayFormatter.FormatCalculationMethod(s.CalculationMethod),
					// Whether landlord still offers this service
					IsActive = s.IsActive,
					// Vietnamese active/inactive label for status column
					StatusDisplay = s.IsActive ? "Đang hoạt động" : "Ngừng hoạt động"
				})
				.ToList();
		}
	}
}
