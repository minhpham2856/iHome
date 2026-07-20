using iHome.DAL.Entities;
using System;

namespace iHome.UI.Views.Manager
{
	internal static class ManagerPageAccess
	{
		public static int GetManagerId(User user)
		{
			if (user.Role != "Manager" || user.Id <= 0)
			{
				throw new UnauthorizedAccessException(
					"Người dùng không có quyền truy cập trang quản lý.");
			}

			return user.Id;
		}
	}
}
