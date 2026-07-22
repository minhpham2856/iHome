namespace iHome.BLL.DTOs.Landlord
{
	// Rental property row for the landlord property list grid.
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

	// Editable property fields for create/edit property dialogs.
	public class PropertyFormDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;
		public string? Description { get; set; }
		public bool IsActive { get; set; } = true;
	}
}
