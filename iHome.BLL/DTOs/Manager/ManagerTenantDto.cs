using System;

namespace iHome.BLL.DTOs
{
	public class ManagerTenantDto
	{
		public int TenantId { get; set; }
		public string FullName { get; set; } = string.Empty;
		public DateOnly DateOfBirth { get; set; }
		public string IdCardNumber { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string? Email { get; set; }
		public string? PermanentAddress { get; set; }
		public int BuildingId { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public int RoomId { get; set; }
		public string RoomNumber { get; set; } = string.Empty;
		public int ContractId { get; set; }
		public bool IsMainTenant { get; set; }
		public string TenantRoleDisplay { get; set; } = string.Empty;
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public string ContractStatus { get; set; } = string.Empty;
		public string ContractStatusDisplay { get; set; } = string.Empty;
	}
}
