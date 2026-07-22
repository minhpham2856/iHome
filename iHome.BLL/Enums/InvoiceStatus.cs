namespace iHome.BLL.Enums
{
	// Invoice.Status values stored in DB and shown in UI (Vietnamese)
	public static class InvoiceStatus
	{
		public const string Paid = "Đã thanh toán";
		public const string Unpaid = "Chưa thanh toán";
		public const string Partial = "Thanh toán một phần";
		public const string Overdue = "Quá hạn";

		// Return status text for UI binding — values are already Vietnamese in DB
		public static string Format(string status) =>
			string.IsNullOrWhiteSpace(status) ? status : status;
	}
}
