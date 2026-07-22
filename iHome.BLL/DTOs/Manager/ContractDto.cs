using iHome.BLL.Enums;
using System;
using System.Collections.Generic;

namespace iHome.BLL.DTOs.Manager
{
	// Dòng lưới hợp đồng Manager (phạm vi tòa được phân công).
	public class ContractDto
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

	// Form tạo/sửa hợp đồng Manager.
	public class ContractFormDto
	{
		public int Id { get; set; }
		public int RoomId { get; set; }
		public int MainTenantId { get; set; }
		public List<int> CoTenantIds { get; set; } = new();
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public decimal MonthlyRent { get; set; }
		public decimal DepositAmount { get; set; }
		public string Status { get; set; } = ContractStatus.Active;
		public string? Notes { get; set; }
	}

	// Tùy chọn hợp đồng cho picker hóa đơn/thanh toán.
	public class ContractOptionDto
	{
		public int Id { get; set; }
		public string DisplayName { get; set; } = string.Empty;
	}
}
