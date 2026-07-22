using System;

namespace iHome.BLL.DTOs.Manager
{
	// Tenant row for the manager tenant list grid, including current lease context.
	public class TenantDto
	{
		// Tenant primary key.
		public int TenantId { get; set; }
		// Legal full name of the tenant.
		public string FullName { get; set; } = string.Empty;
		// Date of birth for identity records.
		public DateOnly DateOfBirth { get; set; }
		// National ID / CCCD number.
		public string IdCardNumber { get; set; } = string.Empty;
		// Primary contact phone number.
		public string PhoneNumber { get; set; } = string.Empty;
		// Optional email address.
		public string? Email { get; set; }
		// Registered permanent home address.
		public string? PermanentAddress { get; set; }
		// Building Id of the tenant's current lease; zero when none.
		public int BuildingId { get; set; }
		// Building name of the current lease.
		public string BuildingName { get; set; } = string.Empty;
		// Room Id of the current lease; zero when none.
		public int RoomId { get; set; }
		// Room number of the current lease.
		public string RoomNumber { get; set; } = string.Empty;
		// Active contract Id linking tenant to the room; zero when no contract.
		public int ContractId { get; set; }
		// True when this tenant is the primary leaseholder on the contract.
		public bool IsMainTenant { get; set; }
		// Vietnamese role label (main tenant vs co-tenant) for the grid.
		public string TenantRoleDisplay { get; set; } = string.Empty;
		// Lease start date on the active contract.
		public DateOnly StartDate { get; set; }
		// Lease end date on the active contract.
		public DateOnly EndDate { get; set; }
		// Raw contract status code.
		public string ContractStatus { get; set; } = string.Empty;
		// Vietnamese contract status label for the grid.
		public string ContractStatusDisplay { get; set; } = string.Empty;
	}
}
