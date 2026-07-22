namespace iHome.BLL.DTOs.Manager
{
	// Room row for the manager room list grid (limited to assigned buildings).
	public class RoomDto
	{
		// Room primary key.
		public int Id { get; set; }
		// Building containing this room.
		public int BuildingId { get; set; }
		// Building name for display in the room grid.
		public string BuildingName { get; set; } = string.Empty;
		// Room number or label.
		public string RoomNumber { get; set; } = string.Empty;
		// Room type category name.
		public string RoomTypeName { get; set; } = string.Empty;
		// Floor level within the building.
		public int Floor { get; set; }
		// Room area in square meters; null when not recorded.
		public decimal? Area { get; set; }
		// Monthly base rent for this room.
		public decimal BaseRent { get; set; }
		// Current number of tenants occupying the room.
		public int CurrentOccupancy { get; set; }
		// Maximum occupants allowed from the room type.
		public int MaxOccupancy { get; set; }
		// Raw status code (Empty, Occupied, Maintenance, …).
		public string Status { get; set; } = string.Empty;
		// Vietnamese status label for grid binding.
		public string StatusDisplay { get; set; } = string.Empty;
		// Optional internal notes about the room.
		public string? Notes { get; set; }
	}
}
