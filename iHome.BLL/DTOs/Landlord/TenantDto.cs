using System;

namespace iHome.BLL.DTOs.Landlord
{
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
		public string CurrentBuildingName { get; set; } = "Chưa gán";
		public string CurrentRoomNumber { get; set; } = "-";
		public string CurrentRoleDisplay { get; set; } = "Chưa gán";
		public string CurrentStatusDisplay { get; set; } = "Chưa có hợp đồng";
		public int ContractCount { get; set; }
	}

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
