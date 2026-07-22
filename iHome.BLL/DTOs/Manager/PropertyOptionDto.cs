namespace iHome.BLL.DTOs.Manager
{
	// Property entry for manager-side filter dropdowns; null Id means "all properties".
	public class PropertyOptionDto
	{
		// Property primary key; null when the option represents every property in scope.
		public int? Id { get; set; }
		// Property name bound to the ComboBox.
		public string Name { get; set; } = string.Empty;
		// Physical address shown as secondary text in pickers when needed.
		public string Address { get; set; } = string.Empty;
	}
}
