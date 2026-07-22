using iHome.BLL.Enums;
using CalcMethod = iHome.BLL.Enums.CalculationMethod;

namespace iHome.BLL.DTOs.Landlord
{
	// Billable utility or amenity service row for the landlord service list grid.
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

	// Editable service fields for create/edit service dialogs.
	public class ServiceFormDto
	{
		public int Id { get; set; }
		public int PropertyId { get; set; }
		public string ServiceName { get; set; } = string.Empty;
		public string Unit { get; set; } = string.Empty;
		public decimal UnitPrice { get; set; }
		public string CalculationMethod { get; set; } = CalcMethod.PerRoom;
		public bool IsActive { get; set; } = true;
	}

	// Calculation method option for service form dropdowns.
	public class CalculationMethodOptionDto
	{
		public string Value { get; set; } = string.Empty;
		public string DisplayName { get; set; } = string.Empty;
	}

	// Room row in the service-to-room assignment summary grid.
	public class RoomServiceSummaryDto
	{
		public int RoomId { get; set; }
		public string RoomNumber { get; set; } = string.Empty;
		public int Floor { get; set; }
		public string AssignedServicesSummary { get; set; } = string.Empty;
		public int AssignedCount { get; set; }
		public string DisplayName => $"Tầng {Floor} - Phòng {RoomNumber}";
	}

	// Checklist item when bulk-assigning services to rooms or rooms to services.
	public class AssignCheckOptionDto
	{
		public int Id { get; set; }
		public string DisplayName { get; set; } = string.Empty;
		public bool IsSelected { get; set; }
	}
}
