namespace iHome.BLL.Enums
{
	// Payment.Method values stored in DB and shown in UI (Vietnamese)
	public static class PaymentMethod
	{
		public const string Cash = "Tiền mặt";
		public const string BankTransfer = "Chuyển khoản";
		public const string Card = "Thẻ";

		// Format payment method for report/grid display
		public static string Format(string? method) =>
			// Missing method → em dash placeholder; otherwise show stored Vietnamese label
			string.IsNullOrWhiteSpace(method) ? "—" : method;
	}
}
