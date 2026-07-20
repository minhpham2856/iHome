using System;

namespace iHome.BLL.Services.Manager
{
	internal static class ManagerServiceGuard
	{
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
