using iHome.BLL.Enums;
using System;
using System.Collections.Generic;

namespace iHome.BLL.DTOs.Manager
{
	// Lease summary row for the manager contract list grid (scoped to assigned buildings).
	public class ContractDto
	{
		// Contract primary key.
		public int Id { get; set; }
		// Leased room Id.
		public int RoomId { get; set; }
		// Building name for display in the contract grid.
		public string BuildingName { get; set; } = string.Empty;
		// Room number for display in the contract grid.
		public string RoomNumber { get; set; } = string.Empty;
		// Lease start date.
		public DateOnly StartDate { get; set; }
		// Lease end date.
		public DateOnly EndDate { get; set; }
		// Agreed monthly rent amount.
		public decimal MonthlyRent { get; set; }
		// Security deposit amount.
		public decimal DepositAmount { get; set; }
		// Raw status code from the database.
		public string Status { get; set; } = string.Empty;
		// Alias of Status for bindings that expect a ContractStatus property name.
		public string ContractStatus => Status;
		// Vietnamese status label for grid binding.
		public string StatusDisplay { get; set; } = string.Empty;
		// Full name of the primary leaseholder.
		public string MainTenantName { get; set; } = string.Empty;
		// Comma-separated names of all tenants on the lease.
		public string TenantNames { get; set; } = string.Empty;
		// Total tenants on this contract.
		public int TenantCount { get; set; }
		// Optional contract notes.
		public string? Notes { get; set; }
	}

	// Payload for creating or editing a contract from the manager UI.
	public class ContractFormDto
	{
		// Contract primary key; zero when creating.
		public int Id { get; set; }
		// Room being leased.
		public int RoomId { get; set; }
		// Primary tenant Id (leaseholder of record).
		public int MainTenantId { get; set; }
		// Khách đứng tên còn lại khi MaxOccupancy > 1 (phòng đôi/nhiều người)
		public List<int> CoTenantIds { get; set; } = new();
		// Lease start date from the form.
		public DateOnly StartDate { get; set; }
		// Lease end date from the form.
		public DateOnly EndDate { get; set; }
		// Monthly rent from the form.
		public decimal MonthlyRent { get; set; }
		// Deposit amount from the form.
		public decimal DepositAmount { get; set; }
		// Contract status from the form; defaults to active for new leases.
		public string Status { get; set; } = ContractStatus.Active;
		// Optional notes from the form.
		public string? Notes { get; set; }
	}

	// Contract entry for invoice or payment contract picker dropdowns.
	public class ContractOptionDto
	{
		// Contract primary key.
		public int Id { get; set; }
		// Combined room/tenant label shown in the ComboBox.
		public string DisplayName { get; set; } = string.Empty;
	}
}
