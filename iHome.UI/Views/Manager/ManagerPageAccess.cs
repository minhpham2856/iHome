using iHome.BLL.Enums;
using iHome.DAL.Entities;
using System;

namespace iHome.UI.Views.Manager
{
	// Kiểm tra user hiện tại là Manager hợp lệ
	internal static class ManagerPageAccess
	{
		// Trả Id manager hoặc ném nếu không đủ quyền
		public static int GetManagerId(User user)
		{
			if (user.Role != UserRole.Manager || user.Id <= 0)
			{
				throw new UnauthorizedAccessException(
					"Người dùng không có quyền truy cập trang quản lý.");
			}

			return user.Id;
		}
	}
}
