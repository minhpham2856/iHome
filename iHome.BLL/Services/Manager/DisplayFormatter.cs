using iHome.BLL.Enums;
using System;

namespace iHome.BLL.Services.Manager
{
	// Map status DB → nhãn tiếng Việt cho UI Manager (values đã là tiếng Việt trong DB)
	internal static class DisplayFormatter
	{
		// Delegate room status formatting to shared RoomStatus enum helper
		public static string FormatRoomStatus(string status) =>
			// Pass stored Vietnamese status through Format (blank → Empty)
			RoomStatus.Format(status);

		// Overload without end date — no ExpiringSoon/Expired derivation
		public static string FormatContractStatus(string status) =>
			// Call full overload with null end date → skips date-based display logic
			FormatContractStatus(status, null);

		// Trạng thái hiển thị theo thời gian thực (không đổi Status trong DB)
		public static string FormatContractStatus(string status, DateOnly? endDate)
		{
			// When EndDate known, derive Expired/ExpiringSoon for Active contracts
			if (endDate.HasValue)
			{
				// Compare status + end date against today without mutating DB
				return ContractStatus.FormatWithEndDate(status, endDate.Value);
			}

			// No end date → return stored status text as-is
			return ContractStatus.Format(status);
		}

		// Format service calculation method for grid/detail display
		public static string FormatCalculationMethod(string method) =>
			// Pass through CalculationMethod.Format (Metered/PerPerson/PerRoom)
			CalculationMethod.Format(method);
	}
}
