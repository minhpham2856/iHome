namespace iHome.BLL.DTOs.Manager
{
	// Billable service row for the manager service catalog grid (read-only view of property services).
	public class ServiceDto
	{
		// Service primary key.
		public int Id { get; set; }
		// Property that owns this service definition.
		public int PropertyId { get; set; }
		// Property name for display in the service grid.
		public string PropertyName { get; set; } = string.Empty;
		// Service name (electricity, water, parking, …).
		public string ServiceName { get; set; } = string.Empty;
		// Billing unit (kWh, m³, person, room, …).
		public string Unit { get; set; } = string.Empty;
		// Price per unit before quantity is applied.
		public decimal UnitPrice { get; set; }
		// Raw calculation method code (Metered, PerRoom, PerPerson, …).
		public string CalculationMethod { get; set; } = string.Empty;
		// Vietnamese label explaining how the service is billed.
		public string CalculationMethodDisplay { get; set; } = string.Empty;
		// Whether the service is available for room assignment and invoicing.
		public bool IsActive { get; set; }
		// Vietnamese active/inactive label for the status column.
		public string StatusDisplay { get; set; } = string.Empty;
	}
}
