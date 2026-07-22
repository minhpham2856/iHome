using iHome.BLL.Enums;
using iHome.DAL.Entities;
using System;

namespace iHome.UI.Views.Manager
{
	// Resolves the current Manager user id for page code-behind guards
	internal static class ManagerPageAccess
	{
		// Return manager id or throw if the signed-in user is not an active Manager
		public static int GetManagerId(User user)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (user.Role != UserRole.Manager || user.Id <= 0)
			{
				// Throw when role guard or required theme resource is missing
				throw new UnauthorizedAccessException(
					// Execute UI step inside GetManagerId
					"Người dùng không có quyền truy cập trang quản lý.");
			}

			// Exit method early or return value/tuple to caller
			return user.Id;
		}
	}
}
