using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	public class ManagerDto
	{
		public int Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? PhoneNumber { get; set; }
		public bool IsActive { get; set; }
		public string StatusDisplay { get; set; } = string.Empty;
		public int PropertyId { get; set; }
		public string PropertyName { get; set; } = string.Empty;
		public string AssignedBuildingsSummary { get; set; } = string.Empty;
		public int AssignedBuildingCount { get; set; }
	}

	public class ManagerFormDto
	{
		public int Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? PhoneNumber { get; set; }
		public bool IsActive { get; set; } = true;
		public int PropertyId { get; set; }
		public List<int> BuildingIds { get; set; } = new();
	}

	public class ManagerPropertyOptionDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	public class ManagerBuildingOptionDto
	{
		public int BuildingId { get; set; }
		public int PropertyId { get; set; }
		public string PropertyName { get; set; } = string.Empty;
		public string BuildingName { get; set; } = string.Empty;
		public bool IsSelected { get; set; }
		public int? CurrentManagerId { get; set; }
		public string? CurrentManagerName { get; set; }
		public bool IsAssignedToOther { get; set; }
		public bool CanSelect => !IsAssignedToOther;
		public string DisplayName =>
			IsAssignedToOther
				? $"{BuildingName} (đang gán: {CurrentManagerName})"
				: BuildingName;
	}

	public class ManagerAccountCreatedDto
	{
		public int UserId { get; set; }
		public string Username { get; set; } = string.Empty;
		public string TemporaryPassword { get; set; } = string.Empty;
		public bool EmailSent { get; set; }
		public string? EmailError { get; set; }
	}
}
