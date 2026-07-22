using System;

namespace iHome.BLL.Enums
{
	// Contract.Status values stored in DB (Vietnamese). ExpiringSoon is display-only.
	public static class ContractStatus
	{
		public const string Active = "Đang hoạt động";
		public const string Expired = "Đã hết hạn";
		public const string Terminated = "Đã chấm dứt";
		public const string ExpiringSoon = "Sắp hết hạn";

		// Return stored status or blank unchanged
		public static string Format(string status) =>
			// Null/whitespace → pass through; otherwise use DB Vietnamese literal
			string.IsNullOrWhiteSpace(status) ? status : status;

		// Alias of Format — kept for call-site readability in reports
		public static string FormatShort(string status) =>
			// Delegate to Format — no end-date logic on short labels
			Format(status);

		// Active + EndDate relative to today → Expired / ExpiringSoon / Active (display only)
		public static string FormatWithEndDate(string status, DateOnly endDate)
		{
			// Only derive display status when DB status is Active — Terminated/Expired stay as stored
			if (string.Equals(status, Active, StringComparison.OrdinalIgnoreCase))
			{
				// Compare contract end date against today's calendar date
				DateOnly today = DateOnly.FromDateTime(DateTime.Today);
				// End date already passed → show as Expired even if DB still says Active
				if (endDate < today)
				{
					return Expired;
				}
				// End date within next calendar month → warn landlord/manager with ExpiringSoon
				if (endDate < today.AddMonths(1))
				{
					return ExpiringSoon;
				}
			}
			// Non-Active or still comfortably within term → use standard Format
			return Format(status);
		}
	}
}
