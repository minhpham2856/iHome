using iHome.BLL.DTOs;
using System;
using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	// Month/year entry for the report period selector dropdown.
	public class ReportMonthOptionDto
	{
		// Calendar year of the report period.
		public int Year { get; set; }
		// Calendar month (1–12) of the report period.
		public int Month { get; set; }
		// Formatted period label (e.g. "07/2026") shown in the ComboBox.
		public string Name { get; set; } = string.Empty;
	}

	// One room line in the room inventory section of a landlord report.
	public class ReportRoomRowDto
	{
		// Property name where the room is located.
		public string PropertyName { get; set; } = string.Empty;
		// Building name within the property.
		public string BuildingName { get; set; } = string.Empty;
		// Room number identifier.
		public string RoomNumber { get; set; } = string.Empty;
		// Floor level of the room.
		public int Floor { get; set; }
		// Room type category (single, double, studio, …).
		public string RoomTypeName { get; set; } = string.Empty;
		// Vietnamese occupancy/status label for the report grid.
		public string StatusDisplay { get; set; } = string.Empty;
		// Listed base rent for the room type.
		public decimal BaseRent { get; set; }
	}

	// One payment line in the revenue section of a landlord report.
	public class ReportRevenueRowDto
	{
		// Date the payment was recorded.
		public DateOnly PaymentDate { get; set; }
		// Formatted payment date for the report grid.
		public string PaymentDateDisplay { get; set; } = string.Empty;
		// Building where the payment was received.
		public string BuildingName { get; set; } = string.Empty;
		// Room associated with the payment.
		public string RoomNumber { get; set; } = string.Empty;
		// Amount paid in this transaction.
		public decimal Amount { get; set; }
		// Vietnamese label for payment method (cash, transfer, …).
		public string MethodDisplay { get; set; } = string.Empty;
	}

	// One invoice line in the invoice section of a landlord report.
	public class ReportInvoiceRowDto
	{
		// Date the invoice was issued.
		public DateOnly InvoiceDate { get; set; }
		// Formatted invoice date for the report grid.
		public string InvoiceDateDisplay { get; set; } = string.Empty;
		// Building billed on this invoice.
		public string BuildingName { get; set; } = string.Empty;
		// Room billed on this invoice.
		public string RoomNumber { get; set; } = string.Empty;
		// Total amount due on the invoice.
		public decimal TotalAmount { get; set; }
		// Vietnamese paid/unpaid/overdue label.
		public string StatusDisplay { get; set; } = string.Empty;
		// Formatted due date for display.
		public string DueDateDisplay { get; set; } = string.Empty;
	}

	// One contract line in the contract section of a landlord report.
	public class ReportContractRowDto
	{
		// Building where the lease applies.
		public string BuildingName { get; set; } = string.Empty;
		// Leased room number.
		public string RoomNumber { get; set; } = string.Empty;
		// Primary tenant on the lease.
		public string MainTenantName { get; set; } = string.Empty;
		// Formatted lease start date.
		public string StartDateDisplay { get; set; } = string.Empty;
		// Formatted lease end date.
		public string EndDateDisplay { get; set; } = string.Empty;
		// Agreed monthly rent.
		public decimal MonthlyRent { get; set; }
		// Vietnamese contract status label.
		public string StatusDisplay { get; set; } = string.Empty;
	}

	// Full report payload: tabular sections plus chart series for the selected period.
	public class ReportDataDto
	{
		// Human-readable label for the selected report period (header/subtitle).
		public string PeriodLabel { get; set; } = string.Empty;
		// Room inventory rows for the period snapshot.
		public List<ReportRoomRowDto> Rooms { get; set; } = new();
		// Payment/revenue rows collected in the period.
		public List<ReportRevenueRowDto> Revenues { get; set; } = new();
		// Invoice rows issued in the period.
		public List<ReportInvoiceRowDto> Invoices { get; set; } = new();
		// Active and historical contract rows in scope.
		public List<ReportContractRowDto> Contracts { get; set; } = new();
		// Revenue trend chart points for the report dashboard.
		public List<ChartPoint> RevenueSeries { get; set; } = new();
		// Room status breakdown chart points for the report dashboard.
		public List<ChartPoint> RoomStatusSeries { get; set; } = new();
	}
}
