using System;
using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	// Lease summary row for the landlord contract list grid.
	public class ContractDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string PropertyName { get; set; } = string.Empty;
		public int BuildingId { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public int RoomId { get; set; }
		public string RoomNumber { get; set; } = string.Empty;
		public string MainTenantName { get; set; } = string.Empty;
		public int TenantCount { get; set; }
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public decimal MonthlyRent { get; set; }
		public decimal DepositAmount { get; set; }
		public string Status { get; set; } = string.Empty;
		public string StatusDisplay { get; set; } = string.Empty;
		public string? Notes { get; set; }
	}

	// Read-only contract fields shown on view/edit forms (location names denormalized for display).
	public class ContractFormDto
	{
		public int Id { get; set; }
		public string PropertyName { get; set; } = string.Empty;
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public string MainTenantName { get; set; } = string.Empty;
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public decimal MonthlyRent { get; set; }
		public decimal DepositAmount { get; set; }
		public string Status { get; set; } = string.Empty;
		public string? Notes { get; set; }
	}

	// One tenant linked to a contract, shown in the contract member list.
	public class ContractTenantMemberDto
	{
		public int TenantId { get; set; }
		public string FullName { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string IdCardNumber { get; set; } = string.Empty;
		public bool IsMainTenant { get; set; }
		public string RoleDisplay => IsMainTenant ? "Người thuê chính" : "Người ở cùng";
	}

	// Status filter or picker option for contract status dropdowns.
	public class ContractStatusOptionDto
	{
		public string Value { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
	}
}
