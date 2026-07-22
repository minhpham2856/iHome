namespace iHome.BLL.DTOs.Manager
{
	// Dòng lưới danh mục dịch vụ Manager (chỉ đọc).
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
	}
}
