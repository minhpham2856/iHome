namespace iHome.BLL.Enums
{
	// Shared Yes/No labels for audit IsActive and bool grid columns
	public static class DisplayText
	{
		public const string Yes = "Có";
		public const string No = "Không";

		// Map bool to Yes/No for AuditDiff and data grids
		public static string FormatActive(bool active) =>
			// true → "Có", false → "Không" for Vietnamese audit/display strings
			active ? Yes : No;
	}
}
