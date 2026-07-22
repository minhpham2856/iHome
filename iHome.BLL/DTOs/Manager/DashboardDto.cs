using System.Collections.Generic;

namespace iHome.BLL.DTOs.Manager
{
	// Aggregated KPI counts, revenue totals, and chart series for the manager dashboard page.
	public class DashboardDto
	{
		// Number of buildings assigned to the logged-in manager.
		public int AssignedBuildings { get; set; }
		// Total rentable rooms across assigned buildings.
		public int TotalRooms { get; set; }
		// Rooms currently occupied by an active contract.
		public int OccupiedRooms { get; set; }
		// Vacant rooms available to lease.
		public int VacantRooms { get; set; }
		// Rooms temporarily unavailable (repairs, cleaning, …).
		public int MaintenanceRooms { get; set; }
		// Tenants with an active lease in assigned buildings.
		public int ActiveTenants { get; set; }
		// Leases currently in effect.
		public int ActiveContracts { get; set; }
		// Contracts nearing end date within the warning window.
		public int ExpiringContracts { get; set; }
		// Invoices past due date and not fully paid.
		public int OverdueInvoices { get; set; }
		// Rent collected in the current calendar month.
		public decimal MonthlyRevenue { get; set; }
		// Remaining unpaid balance on open invoices.
		public decimal OutstandingAmount { get; set; }
		// Monthly revenue trend points for the dashboard chart.
		public List<ChartPointDto> RevenueSeries { get; set; } = new();
		// Room status breakdown points for the pie/donut chart.
		public List<ChartPointDto> RoomStatusSeries { get; set; } = new();
		// Contract status breakdown points for the chart.
		public List<ChartPointDto> ContractStatusSeries { get; set; } = new();
	}
}
