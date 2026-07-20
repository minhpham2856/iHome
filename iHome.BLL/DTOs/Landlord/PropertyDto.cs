namespace iHome.BLL.DTOs.Landlord
{
	public class PropertyDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;
		public string? Description { get; set; }
		public bool IsActive { get; set; }
		public int BuildingCount { get; set; }
		public string StatusDisplay => IsActive ? "Đang hoạt động" : "Ngừng hoạt động";
	}

	public class PropertyFormDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;
		public string? Description { get; set; }
		public bool IsActive { get; set; } = true;
	}
}
