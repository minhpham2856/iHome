namespace iHome.BLL.DTOs.Landlord
{
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

	public class ManagerOptionDto
	{
		public int? Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}
