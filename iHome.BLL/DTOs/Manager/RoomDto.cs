namespace iHome.BLL.DTOs.Manager
{
	// Dòng lưới phòng Manager (tòa được phân công).
	public class RoomDto
	{
		public int Id { get; set; }
		public int BuildingId { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public string RoomTypeName { get; set; } = string.Empty;
		public int Floor { get; set; }
		public decimal? Area { get; set; }
		public decimal BaseRent { get; set; }
		public int CurrentOccupancy { get; set; }
		public int MaxOccupancy { get; set; }
		public string Status { get; set; } = string.Empty;
		public string StatusDisplay { get; set; } = string.Empty;
		public string? Notes { get; set; }
	}
}
