namespace iHome.BLL.DTOs.Manager
{
	// Tùy chọn nhà trọ cho bộ lọc Manager; Id null = tất cả.
	public class PropertyOptionDto
	{
		public int? Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;
	}
}
