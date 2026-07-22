namespace iHome.BLL.DTOs.Manager
{
	// Link between a room and a billable service, shown in the service assignment grid.
	public class RoomServiceAssignmentDto
	{
		// Room receiving the service.
		public int RoomId { get; set; }
		// Service assigned to the room.
		public int ServiceId { get; set; }
		// Building name for context in the assignment list.
		public string BuildingName { get; set; } = string.Empty;
		// Room number for context in the assignment list.
		public string RoomNumber { get; set; } = string.Empty;
		// Service name (electricity, water, …).
		public string ServiceName { get; set; } = string.Empty;
		// Billing unit for the service.
		public string Unit { get; set; } = string.Empty;
		// Price per unit at time of assignment.
		public decimal UnitPrice { get; set; }
		// Whether the assignment is currently active and billable.
		public bool IsActive { get; set; }
		// Vietnamese active/stopped label derived from IsActive for UI binding.
		public string StatusDisplay => IsActive ? "Đang sử dụng" : "Đã ngừng";
	}

	// Generic lookup row for contract/invoice/room picker dropdowns with suggested amounts.
	public class LookupOptionDto
	{
		// Entity primary key (room, tenant, or contract depending on context).
		public int Id { get; set; }
		// Building Id for cascading filters.
		public int BuildingId { get; set; }
		// Property Id for cascading filters.
		public int PropertyId { get; set; }
		// Combined label shown in the ComboBox.
		public string DisplayName { get; set; } = string.Empty;
		// Pre-filled rent or fee amount when this option is selected.
		public decimal SuggestedAmount { get; set; }
		// Sức chứa phòng — dùng khi tạo hợp đồng bắt buộc đủ khách đứng tên
		public int MaxOccupancy { get; set; } = 1;
	}
}
