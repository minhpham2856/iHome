using System;

namespace iHome.BLL.Services.Manager
{
	internal static class ManagerDisplayFormatter
	{
		public static string FormatRoomStatus(string status) => status switch
		{
			"Occupied" => "Đang ở",
			"Vacant" => "Còn trống",
			"Empty" => "Còn trống",
			"Maintenance" => "Bảo trì",
			_ => status
		};

		public static string FormatContractStatus(string status) =>
			FormatContractStatus(status, null);

		// Tính trạng thái hiển thị theo thời gian thực: Active còn < 1 tháng → Sắp hết hạn
		public static string FormatContractStatus(string status, DateOnly? endDate)
		{
			if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) &&
				endDate.HasValue)
			{
				DateOnly today = DateOnly.FromDateTime(DateTime.Today);
				if (endDate.Value < today)
				{
					return "Đã hết hạn";
				}
				if (endDate.Value < today.AddMonths(1))
				{
					return "Sắp hết hạn";
				}
			}

			return status switch
			{
				"Active" => "Đang hoạt động",
				"Expired" => "Đã hết hạn",
				"Terminated" => "Đã chấm dứt",
				_ => status
			};
		}

		public static string FormatCalculationMethod(string method) => method switch
		{
			"Fixed" => "Cố định",
			"Metered" => "Theo chỉ số",
			"PerPerson" => "Theo người",
			"PerRoom" => "Theo phòng",
			_ => method
		};
	}
}
