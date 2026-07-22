using iHome.BLL.DTOs;
using System;
using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	// Month/year entry for the report period selector dropdown.
	public class ReportMonthOptionDto
	{
		public int Year { get; set; }
		public int Month { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	// One room line in the room inventory section of a landlord report.
	public class ReportRoomRowDto
	{
		public string PropertyName { get; set; } = string.Empty;
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public int Floor { get; set; }
		public string RoomTypeName { get; set; } = string.Empty;
		public string StatusDisplay { get; set; } = string.Empty;
		public decimal BaseRent { get; set; }
	}

	// One payment line in the revenue section of a landlord report.
	public class ReportRevenueRowDto
	{
		public DateOnly PaymentDate { get; set; }
		public string PaymentDateDisplay { get; set; } = string.Empty;
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public string MethodDisplay { get; set; } = string.Empty;
	}

	// One invoice line in the invoice section of a landlord report.
	public class ReportInvoiceRowDto
	{
		public DateOnly InvoiceDate { get; set; }
		public string InvoiceDateDisplay { get; set; } = string.Empty;
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public decimal TotalAmount { get; set; }
		public string StatusDisplay { get; set; } = string.Empty;
		public string DueDateDisplay { get; set; } = string.Empty;
	}

	// One contract line in the contract section of a landlord report.
	public class ReportContractRowDto
	{
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public string MainTenantName { get; set; } = string.Empty;
		public string StartDateDisplay { get; set; } = string.Empty;
		public string EndDateDisplay { get; set; } = string.Empty;
		public decimal MonthlyRent { get; set; }
		public string StatusDisplay { get; set; } = string.Empty;
	}

	// Full report payload: tabular sections plus chart series for the selected period.
	public class ReportDataDto
	{
		public string PeriodLabel { get; set; } = string.Empty;
		public List<ReportRoomRowDto> Rooms { get; set; } = new();
		public List<ReportRevenueRowDto> Revenues { get; set; } = new();
		public List<ReportInvoiceRowDto> Invoices { get; set; } = new();
		public List<ReportContractRowDto> Contracts { get; set; } = new();
		public List<ChartPoint> RevenueSeries { get; set; } = new();
		public List<ChartPoint> RoomStatusSeries { get; set; } = new();
	}
}
