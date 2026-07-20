using System;

namespace iHome.BLL.DTOs
{
	public class ManagerContractDto
	{
		public int Id { get; set; }
		public int RoomId { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public decimal MonthlyRent { get; set; }
		public decimal DepositAmount { get; set; }
		public string Status { get; set; } = string.Empty;
		public string ContractStatus => Status;
		public string StatusDisplay { get; set; } = string.Empty;
		public string MainTenantName { get; set; } = string.Empty;
		public string TenantNames { get; set; } = string.Empty;
		public int TenantCount { get; set; }
		public string? Notes { get; set; }
	}

	public class ManagerContractFormDto
	{
		public int Id { get; set; }
		public int RoomId { get; set; }
		public int MainTenantId { get; set; }
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public decimal MonthlyRent { get; set; }
		public decimal DepositAmount { get; set; }
		public string Status { get; set; } = "Active";
		public string? Notes { get; set; }
	}

	public class ManagerContractOptionDto
	{
		public int Id { get; set; }
		public string DisplayName { get; set; } = string.Empty;
	}
}
