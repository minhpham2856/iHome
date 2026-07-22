namespace iHome.BLL.DTOs.Landlord
{
	// Building row for the landlord building list and detail views.
	public class BuildingDto
	{
		// Building primary key.
		public int Id { get; set; }
		// Parent property this building belongs to.
		public int PropertyId { get; set; }
		// Building name shown in grids and headers.
		public string Name { get; set; } = string.Empty;
		// Number of floors in the building (used for room floor validation).
		public int NumberOfFloors { get; set; }
		// Optional notes about the building.
		public string? Description { get; set; }
		// Assigned manager user Id; null when no manager is assigned.
		public int? ManagerId { get; set; }
		// Display name of the assigned manager, or default when unassigned.
		public string ManagerName { get; set; } = "Chưa gán";
		// Whether the building is open for leasing and management.
		public bool IsActive { get; set; }
		// Vietnamese active/inactive label derived from IsActive for UI binding.
		public string StatusDisplay => IsActive ? "Đang hoạt động" : "Ngừng hoạt động";
	}

	// Editable building fields for create/edit building dialogs.
	public class BuildingFormDto
	{
		// Building primary key; zero when creating a new building.
		public int Id { get; set; }
		// Property the building will be created under.
		public int PropertyId { get; set; }
		// Building name entered on the form.
		public string Name { get; set; } = string.Empty;
		// Floor count; defaults to one for new buildings.
		public int NumberOfFloors { get; set; } = 1;
		// Optional description from the form.
		public string? Description { get; set; }
		// Selected manager to assign; null clears assignment.
		public int? ManagerId { get; set; }
		// Active flag from the form checkbox; defaults to true for new records.
		public bool IsActive { get; set; } = true;
	}

	// Manager entry for building assignment dropdowns.
	public class ManagerOptionDto
	{
		// Manager user Id; null for placeholder options.
		public int? Id { get; set; }
		// Manager full name shown in the ComboBox.
		public string Name { get; set; } = string.Empty;
	}
}
