using System.Collections.Generic;

namespace iHome.BLL.DTOs
{
	public class ChartPoint
	{
		public string Label { get; set; } = string.Empty;
		public double Value { get; set; }
	}

	public class DashboardPropertyOption
	{
		public int? Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	public class DashboardData
	{
		public int TotalProperties { get; set; }
		public int TotalBuildings { get; set; }
		public int TotalRooms { get; set; }
		public int OccupiedRooms { get; set; }
		public int VacantRooms { get; set; }
		public int TotalTenants { get; set; }
		public int ActiveContracts { get; set; }
		public decimal MonthlyRevenue { get; set; }
		public decimal OutstandingAmount { get; set; }
		public int OverdueInvoices { get; set; }

		public List<ChartPoint> RevenueSeries { get; set; } = new();
		public List<ChartPoint> RoomStatusSeries { get; set; } = new();
		public List<ChartPoint> ContractStatusSeries { get; set; } = new();
	}
}
