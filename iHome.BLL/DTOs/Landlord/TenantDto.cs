using System;

namespace iHome.BLL.DTOs.Landlord
{
	// Tenant profile row for the landlord tenant directory grid.
	public class TenantDto
	{
		public int Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public DateOnly DateOfBirth { get; set; }
		public string IdCardNumber { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string? Email { get; set; }
		public string? PermanentAddress { get; set; }
		public string CurrentPropertyName { get; set; } = "-";
		public int CurrentPropertyId { get; set; }
		public string CurrentBuildingName { get; set; } = "Chưa gán";
		public int CurrentBuildingId { get; set; }
		public string CurrentRoomNumber { get; set; } = "-";
		public string CurrentRoleDisplay { get; set; } = "Chưa gán";
		public string CurrentStatusDisplay { get; set; } = "Chưa có hợp đồng";
		public int ContractCount { get; set; }
	}

	// Editable tenant identity fields for create/edit tenant dialogs.
	public class TenantFormDto
	{
		public int Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public DateOnly DateOfBirth { get; set; }
		public string IdCardNumber { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string? Email { get; set; }
		public string? PermanentAddress { get; set; }
	}

	// One lease history row on the tenant detail contract list.
	public class TenantContractDto
	{
		public int ContractId { get; set; }
		public string PropertyName { get; set; } = string.Empty;
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public bool IsMainTenant { get; set; }
		public string RoleDisplay => IsMainTenant ? "Người thuê chính" : "Người ở cùng";
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public string Status { get; set; } = string.Empty;
		public string StatusDisplay { get; set; } = string.Empty;
		public decimal MonthlyRent { get; set; }
	}
}
