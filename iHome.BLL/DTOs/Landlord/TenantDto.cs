using System;

namespace iHome.BLL.DTOs.Landlord
{
	// Tenant profile row for the landlord tenant directory grid.
	public class TenantDto
	{
		// Tenant primary key.
		public int Id { get; set; }
		// Legal full name of the tenant.
		public string FullName { get; set; } = string.Empty;
		// Date of birth for identity verification.
		public DateOnly DateOfBirth { get; set; }
		// National ID / CCCD number.
		public string IdCardNumber { get; set; } = string.Empty;
		// Primary contact phone number.
		public string PhoneNumber { get; set; } = string.Empty;
		// Optional email address.
		public string? Email { get; set; }
		// Registered permanent home address.
		public string? PermanentAddress { get; set; }
		// Name of the property where the tenant currently leases; "-" when none.
		public string CurrentPropertyName { get; set; } = "-";
		// Property Id of the current lease; zero when not leasing.
		public int CurrentPropertyId { get; set; }
		// Building name of the current lease; default when unassigned.
		public string CurrentBuildingName { get; set; } = "Chưa gán";
		// Building Id of the current lease; zero when not leasing.
		public int CurrentBuildingId { get; set; }
		// Room number of the current lease; "-" when none.
		public string CurrentRoomNumber { get; set; } = "-";
		// Vietnamese label for tenant role on the active contract.
		public string CurrentRoleDisplay { get; set; } = "Chưa gán";
		// Vietnamese label for lease status (active, none, …).
		public string CurrentStatusDisplay { get; set; } = "Chưa có hợp đồng";
		// Total contracts this tenant has ever been linked to.
		public int ContractCount { get; set; }
	}

	// Editable tenant identity fields for create/edit tenant dialogs.
	public class TenantFormDto
	{
		// Tenant primary key; zero when creating.
		public int Id { get; set; }
		// Full name from the form.
		public string FullName { get; set; } = string.Empty;
		// Date of birth from the form.
		public DateOnly DateOfBirth { get; set; }
		// ID card number from the form.
		public string IdCardNumber { get; set; } = string.Empty;
		// Phone number from the form.
		public string PhoneNumber { get; set; } = string.Empty;
		// Optional email from the form.
		public string? Email { get; set; }
		// Optional permanent address from the form.
		public string? PermanentAddress { get; set; }
	}

	// One lease history row on the tenant detail contract list.
	public class TenantContractDto
	{
		// Contract primary key.
		public int ContractId { get; set; }
		// Property name for this lease.
		public string PropertyName { get; set; } = string.Empty;
		// Building name for this lease.
		public string BuildingName { get; set; } = string.Empty;
		// Room number for this lease.
		public string RoomNumber { get; set; } = string.Empty;
		// True when this tenant was the primary leaseholder on the contract.
		public bool IsMainTenant { get; set; }
		// Vietnamese role label (main tenant vs co-tenant) for the history grid.
		public string RoleDisplay => IsMainTenant ? "Người thuê chính" : "Người ở cùng";
		// Lease start date.
		public DateOnly StartDate { get; set; }
		// Lease end date.
		public DateOnly EndDate { get; set; }
		// Raw contract status code.
		public string Status { get; set; } = string.Empty;
		// Vietnamese status label for the history grid.
		public string StatusDisplay { get; set; } = string.Empty;
		// Monthly rent on this contract.
		public decimal MonthlyRent { get; set; }
	}
}
