using System.Collections.Generic;

namespace iHome.BLL.DTOs
{
	// Single label/value pair for LiveCharts series on the shared dashboard.
	public class ChartPoint
	{
		// Category label shown on the chart axis or legend (e.g. month name, status name).
		public string Label { get; set; } = string.Empty;
		// Numeric magnitude plotted for this label (revenue amount, room count, etc.).
		public double Value { get; set; }
	}

	// Property entry for the dashboard property filter ComboBox; null Id means "all properties".
	public class DashboardPropertyOption
	{
		// Property primary key; null when the option represents every property.
		public int? Id { get; set; }
		// Human-readable property name bound to the filter dropdown.
		public string Name { get; set; } = string.Empty;
	}

	// Aggregated KPI counts, revenue totals, and chart series for the role dashboard page.
	public class DashboardData
	{
		// Total rental properties owned or visible to the current user.
		public int TotalProperties { get; set; }
		// Total buildings across all included properties.
		public int TotalBuildings { get; set; }
		// Total rentable rooms in scope for the selected filter.
		public int TotalRooms { get; set; }
		// Rooms currently occupied by an active contract.
		public int OccupiedRooms { get; set; }
		// Rooms with no active tenant (available to rent).
		public int VacantRooms { get; set; }
		// Distinct tenants with at least one active lease.
		public int TotalTenants { get; set; }
		// Leases currently in effect (not expired or terminated).
		public int ActiveContracts { get; set; }
		// Sum of rent collected in the current calendar month.
		public decimal MonthlyRevenue { get; set; }
		// Unpaid invoice balance still owed by tenants.
		public decimal OutstandingAmount { get; set; }
		// Count of invoices past their due date and not fully paid.
		public int OverdueInvoices { get; set; }

		// Monthly revenue trend points for the revenue line/bar chart.
		public List<ChartPoint> RevenueSeries { get; set; } = new();
		// Occupied vs vacant (and other status) breakdown for the room-status pie chart.
		public List<ChartPoint> RoomStatusSeries { get; set; } = new();
		// Active vs expired contract counts for the contract-status chart.
		public List<ChartPoint> ContractStatusSeries { get; set; } = new();
	}
}
