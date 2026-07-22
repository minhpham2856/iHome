using System;
using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	// Lease summary row for the landlord contract list grid.
	public class ContractDto
	{
		// Contract primary key.
		public int Id { get; set; }
		// Property containing the leased room.
		public int PropertyId { get; set; }
		// Property name for display in the contract grid.
		public string PropertyName { get; set; } = string.Empty;
		// Building containing the leased room.
		public int BuildingId { get; set; }
		// Building name for display in the contract grid.
		public string BuildingName { get; set; } = string.Empty;
		// Leased room Id.
		public int RoomId { get; set; }
		// Room number shown alongside building/property.
		public string RoomNumber { get; set; } = string.Empty;
		// Full name of the primary leaseholder.
		public string MainTenantName { get; set; } = string.Empty;
		// Total tenants on this contract (main + co-tenants).
		public int TenantCount { get; set; }
		// Lease start date.
		public DateOnly StartDate { get; set; }
		// Lease end date.
		public DateOnly EndDate { get; set; }
		// Agreed monthly rent amount.
		public decimal MonthlyRent { get; set; }
		// Security deposit collected at signing.
		public decimal DepositAmount { get; set; }
		// Raw status code from the database (Active, Expired, …).
		public string Status { get; set; } = string.Empty;
		// Vietnamese status label for grid and detail binding.
		public string StatusDisplay { get; set; } = string.Empty;
		// Optional contract notes.
		public string? Notes { get; set; }
	}

	// Read-only contract fields shown on view/edit forms (location names denormalized for display).
	public class ContractFormDto
	{
		// Contract primary key.
		public int Id { get; set; }
		// Property name (read-only on form).
		public string PropertyName { get; set; } = string.Empty;
		// Building name (read-only on form).
		public string BuildingName { get; set; } = string.Empty;
		// Room number (read-only on form).
		public string RoomNumber { get; set; } = string.Empty;
		// Primary tenant name (read-only on form).
		public string MainTenantName { get; set; } = string.Empty;
		// Lease start date editable on the form.
		public DateOnly StartDate { get; set; }
		// Lease end date editable on the form.
		public DateOnly EndDate { get; set; }
		// Monthly rent amount on the form.
		public decimal MonthlyRent { get; set; }
		// Deposit amount on the form.
		public decimal DepositAmount { get; set; }
		// Contract status selected or displayed on the form.
		public string Status { get; set; } = string.Empty;
		// Optional notes from the form.
		public string? Notes { get; set; }
	}

	// One tenant linked to a contract, shown in the contract member list.
	public class ContractTenantMemberDto
	{
		// Tenant primary key.
		public int TenantId { get; set; }
		// Tenant full name.
		public string FullName { get; set; } = string.Empty;
		// Contact phone number.
		public string PhoneNumber { get; set; } = string.Empty;
		// National ID / CCCD number.
		public string IdCardNumber { get; set; } = string.Empty;
		// True when this tenant is the primary leaseholder.
		public bool IsMainTenant { get; set; }
		// Vietnamese role label (main tenant vs co-tenant) for the member grid.
		public string RoleDisplay => IsMainTenant ? "Người thuê chính" : "Người ở cùng";
	}

	// Status filter or picker option for contract status dropdowns.
	public class ContractStatusOptionDto
	{
		// Internal status code stored in the database.
		public string Value { get; set; } = string.Empty;
		// Vietnamese label shown in the ComboBox.
		public string Name { get; set; } = string.Empty;
	}
}
