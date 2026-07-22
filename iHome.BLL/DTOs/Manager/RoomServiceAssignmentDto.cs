namespace iHome.BLL.DTOs.Manager
{
	// Liên kết phòng–dịch vụ trên lưới gán dịch vụ.
	public class RoomServiceAssignmentDto
	{
		public int RoomId { get; set; }
		public int ServiceId { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public string ServiceName { get; set; } = string.Empty;
		public string Unit { get; set; } = string.Empty;
		public decimal UnitPrice { get; set; }
		public bool IsActive { get; set; }
		public string StatusDisplay => IsActive ? "Đang sử dụng" : "Đã ngừng";
	}

	// Dòng lookup chung cho picker HĐ/hóa đơn/phòng (kèm số gợi ý).
	public class LookupOptionDto
	{
		public int Id { get; set; }
		public int BuildingId { get; set; }
		public int PropertyId { get; set; }
		public string DisplayName { get; set; } = string.Empty;
		public decimal SuggestedAmount { get; set; }
		public int MaxOccupancy { get; set; } = 1;
	}
}
