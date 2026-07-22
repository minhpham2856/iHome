namespace iHome.BLL.DTOs.Manager
{
	// Tùy chọn tòa cho bộ lọc/picker Manager; Id null = tất cả tòa.
	public class BuildingOptionDto
	{
		public int? Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}
