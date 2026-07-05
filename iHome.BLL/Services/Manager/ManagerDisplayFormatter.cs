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

		public static string FormatContractStatus(string status) => status switch
		{
			"Active" => "Đang hoạt động",
			"Expired" => "Đã hết hạn",
			"Terminated" => "Đã chấm dứt",
			_ => status
		};

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
