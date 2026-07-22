using iHome.BLL.Enums;

namespace iHome.BLL.Services.Manager
{
	// Map status DB → nhãn tiếng Việt cho UI Manager
	internal static class DisplayFormatter
	{
		public static string FormatRoomStatus(string status) =>
			RoomStatus.Format(status);

		public static string FormatContractStatus(string status) =>
			FormatContractStatus(status, null);

		// Có EndDate: suy ra Sắp hết hạn / Hết hạn theo ngày thực tế (không đổi Status DB)
		public static string FormatContractStatus(string status, DateOnly? endDate)
		{
			if (endDate.HasValue)
			{
				return ContractStatus.FormatWithEndDate(status, endDate.Value);
			}

			return ContractStatus.Format(status);
		}

		public static string FormatCalculationMethod(string method) =>
			CalculationMethod.Format(method);
	}
}
