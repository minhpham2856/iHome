namespace iHome.BLL.DTOs.Landlord
{
	public class ServiceDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string PropertyName { get; set; } = string.Empty;
		public string ServiceName { get; set; } = string.Empty;
		public string Unit { get; set; } = string.Empty;
		public decimal UnitPrice { get; set; }
		public string CalculationMethod { get; set; } = string.Empty;
		public string CalculationMethodDisplay { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public string StatusDisplay { get; set; } = string.Empty;
		public int AssignmentCount { get; set; }
	}

	public class ServiceFormDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string ServiceName { get; set; } = string.Empty;
		public string Unit { get; set; } = string.Empty;
		public decimal UnitPrice { get; set; }
		public string CalculationMethod { get; set; } = "PerRoom";
		public bool IsActive { get; set; } = true;
	}

	public class CalculationMethodOptionDto
	{
		public string Value { get; set; } = string.Empty;
		public string DisplayName { get; set; } = string.Empty;
	}

	public class RoomServiceSummaryDto
	{
		public int RoomId { get; set; }
		public string RoomNumber { get; set; } = string.Empty;
		public int Floor { get; set; }
		public string AssignedServicesSummary { get; set; } = string.Empty;
		public int AssignedCount { get; set; }
		public string DisplayName => $"Tầng {Floor} - Phòng {RoomNumber}";
	}

	public class AssignCheckOptionDto
	{
		public int Id { get; set; }
		public string DisplayName { get; set; } = string.Empty;
		public bool IsSelected { get; set; }
	}
}
