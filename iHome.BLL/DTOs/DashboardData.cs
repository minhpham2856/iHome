using System.Collections.Generic;

namespace iHome.BLL.DTOs
{
	// a single labelled value, used by every dashboard chart series
	public class ChartPoint
	{
		public string Label { get; set; } = string.Empty;
		public double Value { get; set; }
	}

	// aggregated data returned by DashboardService and bound by the dashboard UI
	public class DashboardData
	{
		// KPI values shown in the top card row
		public int TotalBuildings { get; set; }
		public int TotalRooms { get; set; }
		public int OccupiedRooms { get; set; }
		public int VacantRooms { get; set; }
		public int TotalTenants { get; set; }
		public int ActiveContracts { get; set; }
		public decimal MonthlyRevenue { get; set; }
		public decimal OutstandingAmount { get; set; }
		public int OverdueInvoices { get; set; }
		public int ExpiringContracts { get; set; }

		// chart series (each a list of labelled values)
		public List<ChartPoint> RevenueSeries { get; set; } = new List<ChartPoint>();
		public List<ChartPoint> RoomStatusSeries { get; set; } = new List<ChartPoint>();
		public List<ChartPoint> ContractStatusSeries { get; set; } = new List<ChartPoint>();
	}
}
