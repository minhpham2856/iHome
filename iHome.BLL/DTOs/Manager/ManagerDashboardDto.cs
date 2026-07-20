using System.Collections.Generic;

namespace iHome.BLL.DTOs
{
	public class ManagerDashboardDto
	{
		public int AssignedBuildings { get; set; }
		public int TotalRooms { get; set; }
		public int OccupiedRooms { get; set; }
		public int VacantRooms { get; set; }
		public int MaintenanceRooms { get; set; }
		public int ActiveTenants { get; set; }
		public int ActiveContracts { get; set; }
		public int ExpiringContracts { get; set; }
		public int OverdueInvoices { get; set; }
		public decimal MonthlyRevenue { get; set; }
		public decimal OutstandingAmount { get; set; }
		public List<ManagerChartPointDto> RevenueSeries { get; set; } = new();
		public List<ManagerChartPointDto> RoomStatusSeries { get; set; } = new();
		public List<ManagerChartPointDto> ContractStatusSeries { get; set; } = new();
	}
}
