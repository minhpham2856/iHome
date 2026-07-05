namespace iHome.BLL.DTOs
{
	public class ManagerRoomServiceAssignmentDto
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

	public class ManagerLookupOptionDto
	{
		public int Id { get; set; }
		public int BuildingId { get; set; }
		public string DisplayName { get; set; } = string.Empty;
		public decimal SuggestedAmount { get; set; }
	}
}
