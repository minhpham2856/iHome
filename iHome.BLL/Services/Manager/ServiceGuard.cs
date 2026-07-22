using System;

namespace iHome.BLL.Services.Manager
{
	// Kiểm tra tham số chung cho service Manager
	internal static class ServiceGuard
	{
		// Từ chối managerId không hợp lệ trước khi truy vấn
		public static void EnsureValidManagerId(int managerId)
		{
			if (managerId <= 0)
			{
				throw new ArgumentOutOfRangeException(
					nameof(managerId),
					"Mã quản lý phải lớn hơn 0.");
			}
		}
	}
}
