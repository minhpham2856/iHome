namespace iHome.BLL.DTOs.Landlord
{
	// Rental property row for the landlord property list grid.
	public class PropertyDto
	{
		// Property primary key.
		public int Id { get; set; }
		// Commercial or site name of the rental property.
		public string Name { get; set; } = string.Empty;
		// Physical address shown to users and on reports.
		public string Address { get; set; } = string.Empty;
		// Optional description or marketing notes.
		public string? Description { get; set; }
		// Whether the property accepts new leases and appears in active lists.
		public bool IsActive { get; set; }
		// Number of buildings under this property (denormalized for the grid).
		public int BuildingCount { get; set; }
		// Vietnamese active/inactive label derived from IsActive for UI binding.
		public string StatusDisplay => IsActive ? "Đang hoạt động" : "Ngừng hoạt động";
	}

	// Editable property fields for create/edit property dialogs.
	public class PropertyFormDto
	{
		// Property primary key; zero when creating a new property.
		public int Id { get; set; }
		// Property name from the form.
		public string Name { get; set; } = string.Empty;
		// Street address from the form.
		public string Address { get; set; } = string.Empty;
		// Optional description from the form.
		public string? Description { get; set; }
		// Active flag from the form; defaults to true for new properties.
		public bool IsActive { get; set; } = true;
	}
}
