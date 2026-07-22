namespace iHome.BLL.DTOs.Landlord
{
	// Building row for the landlord building list and detail views.
	public class BuildingDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string Name { get; set; } = string.Empty;
		public int NumberOfFloors { get; set; }
		public string? Description { get; set; }
		public int? ManagerId { get; set; }
		public string ManagerName { get; set; } = "Chưa gán";
		public bool IsActive { get; set; }
		public string StatusDisplay => IsActive ? "Đang hoạt động" : "Ngừng hoạt động";
	}

	// Editable building fields for create/edit building dialogs.
	public class BuildingFormDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string Name { get; set; } = string.Empty;
		public int NumberOfFloors { get; set; } = 1;
		public string? Description { get; set; }
		public int? ManagerId { get; set; }
		public bool IsActive { get; set; } = true;
	}

	// Manager entry for building assignment dropdowns.
	public class ManagerOptionDto
	{
		public int? Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}
