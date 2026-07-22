namespace iHome.BLL.Enums
{
	// Services.CalculationMethod values stored in DB and shown in UI (Vietnamese)
	public static class CalculationMethod
	{
		public const string Metered = "Theo chỉ số";
		public const string PerPerson = "Theo người";
		public const string PerRoom = "Theo phòng";

		// Pass through stored method text; blank stays blank for grid cells
		public static string Format(string method) =>
			string.IsNullOrWhiteSpace(method) ? method : method;
	}
}
