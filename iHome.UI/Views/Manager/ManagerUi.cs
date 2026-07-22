using System;
using System.Windows;

namespace iHome.UI.Views.Manager
{
	// Helper UI Manager: bắt lỗi nghiệp vụ và hiện MessageBox
	internal static class ManagerUi
	{
		// Cảnh báo dữ liệu không hợp lệ
		public static void ShowValidation(string message) =>
			MessageBox.Show(message, "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);

		// Cảnh báo thao tác thất bại
		public static void ShowError(string message) =>
			MessageBox.Show(message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);

		// Chạy thao tác ghi; false nếu đã hiện lỗi nghiệp vụ
		public static bool TryRun(Action action)
		{
			string? error = Execute(action);
			if (error == null)
			{
				return true;
			}
			ShowError(error);
			return false;
		}

		// Chạy thao tác có trả về giá trị
		public static (bool ok, T value) TryRun<T>(Func<T> action)
		{
			try
			{
				return (true, action());
			}
			catch (Exception ex) when (IsBusinessError(ex))
			{
				ShowError(ex.Message);
				return (false, default!);
			}
		}

		// Tải dữ liệu; caller tự xử lý MessageBox khi thất bại
		public static (bool ok, T? value, string? error) TryGet<T>(Func<T> action)
		{
			try
			{
				return (true, action(), null);
			}
			catch (Exception ex) when (IsBusinessError(ex))
			{
				return (false, default, ex.Message);
			}
		}

		// Chạy action; trả message lỗi hoặc null nếu thành công
		private static string? Execute(Action action)
		{
			try
			{
				action();
				return null;
			}
			catch (Exception ex) when (IsBusinessError(ex))
			{
				return ex.Message;
			}
		}

		// Exception nghiệp vụ BLL → hiện cho người dùng
		private static bool IsBusinessError(Exception ex) =>
			ex is ArgumentException
				or ArgumentOutOfRangeException
				or InvalidOperationException
				or UnauthorizedAccessException;
	}
}
