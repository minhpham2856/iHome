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
			string.IsNullOrWhiteSpace(status) ? status : status;

		// Alias of Format — kept for call-site readability in reports
		public static string FormatShort(string status) =>
			Format(status);

		// Active + EndDate relative to today → Expired / ExpiringSoon / Active (display only)
		public static string FormatWithEndDate(string status, DateOnly endDate)
		{
			if (string.Equals(status, Active, StringComparison.OrdinalIgnoreCase))
			{
				DateOnly today = DateOnly.FromDateTime(DateTime.Today);
				if (endDate < today)
				{
					return Expired;
				}
				if (endDate < today.AddMonths(1))
				{
					return ExpiringSoon;
				}
			}
			return Format(status);
		}
	}
}
