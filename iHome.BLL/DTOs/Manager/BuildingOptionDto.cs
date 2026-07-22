namespace iHome.BLL.DTOs.Manager
{
	// Building entry for manager-side filter and picker dropdowns; null Id means "all buildings".
	public class BuildingOptionDto
	{
		// Building primary key; null when the option represents every assigned building.
		public int? Id { get; set; }
		// Building name bound to the ComboBox.
		public string Name { get; set; } = string.Empty;
	}
}
