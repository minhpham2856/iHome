namespace iHome.BLL.DTOs
{
	public class ManagerServiceDto
	{
		public int Id { get; set; }
		public int BuildingId { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public string ServiceName { get; set; } = string.Empty;
		public string Unit { get; set; } = string.Empty;
		public decimal UnitPrice { get; set; }
		public string CalculationMethod { get; set; } = string.Empty;
		public string CalculationMethodDisplay { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public string StatusDisplay { get; set; } = string.Empty;
	}
}
