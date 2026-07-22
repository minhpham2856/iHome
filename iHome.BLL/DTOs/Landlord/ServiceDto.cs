using iHome.BLL.Enums;
using CalcMethod = iHome.BLL.Enums.CalculationMethod;

namespace iHome.BLL.DTOs.Landlord
{
	// Billable utility or amenity service row for the landlord service list grid.
	public class ServiceDto
	{
		// Service primary key.
		public int Id { get; set; }
		// Property that defines and owns this service catalog entry.
		public int PropertyId { get; set; }
		// Property name for display in the service grid.
		public string PropertyName { get; set; } = string.Empty;
		// Service name (electricity, water, parking, …).
		public string ServiceName { get; set; } = string.Empty;
		// Billing unit (kWh, m³, person, room, …).
		public string Unit { get; set; } = string.Empty;
		// Price per unit before quantity or meter delta is applied.
		public decimal UnitPrice { get; set; }
		// Raw calculation method code (Metered, PerRoom, PerPerson, …).
		public string CalculationMethod { get; set; } = string.Empty;
		// Vietnamese label explaining how the service is billed.
		public string CalculationMethodDisplay { get; set; } = string.Empty;
		// Whether the service can be assigned to rooms and appear on invoices.
		public bool IsActive { get; set; }
		// Vietnamese active/inactive label for the status column.
		public string StatusDisplay { get; set; } = string.Empty;
		// Number of rooms currently assigned this service.
		public int AssignmentCount { get; set; }
	}

	// Editable service fields for create/edit service dialogs.
	public class ServiceFormDto
	{
		// Service primary key; zero when creating.
		public int Id { get; set; }
		// Property the service belongs to.
		public int PropertyId { get; set; }
		// Service name from the form.
		public string ServiceName { get; set; } = string.Empty;
		// Billing unit from the form.
		public string Unit { get; set; } = string.Empty;
		// Unit price from the form.
		public decimal UnitPrice { get; set; }
		// Calculation method from the form; defaults to per-room billing.
		public string CalculationMethod { get; set; } = CalcMethod.PerRoom;
		// Active flag from the form; defaults to true for new services.
		public bool IsActive { get; set; } = true;
	}

	// Calculation method option for service form dropdowns.
	public class CalculationMethodOptionDto
	{
		// Internal method code stored on the service.
		public string Value { get; set; } = string.Empty;
		// Vietnamese description shown in the ComboBox.
		public string DisplayName { get; set; } = string.Empty;
	}

	// Room row in the service-to-room assignment summary grid.
	public class RoomServiceSummaryDto
	{
		// Room primary key.
		public int RoomId { get; set; }
		// Room number for identification.
		public string RoomNumber { get; set; } = string.Empty;
		// Floor level of the room.
		public int Floor { get; set; }
		// Comma-separated names of services assigned to this room.
		public string AssignedServicesSummary { get; set; } = string.Empty;
		// Count of services linked to the room.
		public int AssignedCount { get; set; }
		// Combined floor and room label for assignment picker lists.
		public string DisplayName => $"Tầng {Floor} - Phòng {RoomNumber}";
	}

	// Checklist item when bulk-assigning services to rooms or rooms to services.
	public class AssignCheckOptionDto
	{
		// Entity Id (room or service depending on context).
		public int Id { get; set; }
		// Label shown next to the checkbox.
		public string DisplayName { get; set; } = string.Empty;
		// Whether the checkbox is ticked on the assignment form.
		public bool IsSelected { get; set; }
	}
}
