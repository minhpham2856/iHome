using System.Collections.Generic;

namespace iHome.BLL.DTOs.Manager
{
	// KPI, doanh thu và chuỗi biểu đồ dashboard Manager.
	public class DashboardDto
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
		public List<ChartPointDto> RevenueSeries { get; set; } = new();
		public List<ChartPointDto> RoomStatusSeries { get; set; } = new();
		public List<ChartPointDto> ContractStatusSeries { get; set; } = new();
	}
}
