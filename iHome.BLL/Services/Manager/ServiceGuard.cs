using System;

namespace iHome.BLL.Services.Manager
{
	// Shared argument guards for Manager BLL services
	internal static class ServiceGuard
	{
		// Reject invalid manager ids before any scoped query runs
		public static void EnsureValidManagerId(int managerId)
		{
			// ManagerId must be positive — zero/negative means unauthenticated or corrupt session
			if (managerId <= 0)
			{
				// Throw with Vietnamese message — UI may surface via MessageBox
				throw new ArgumentOutOfRangeException(
					nameof(managerId),
					"Mã quản lý phải lớn hơn 0.");
			}
		}
	}
}
