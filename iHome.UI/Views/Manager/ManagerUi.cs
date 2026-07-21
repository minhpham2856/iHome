using System;
using System.Threading.Tasks;
using System.Windows;

namespace iHome.UI.Views.Manager
{
	// Helper UI Manager: hiện MessageBox lỗi trên form thay vì để exception nhảy vào debugger (Continue)
	// TryRunAsync / TryGetAsync bắt lỗi nghiệp vụ bên trong Task.Run rồi trả message
	internal static class ManagerUi
	{
		public static void ShowValidation(string message) =>
			MessageBox.Show(message, "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);

		public static void ShowError(string message) =>
			MessageBox.Show(message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);

		// Chạy thao tác ghi (create/update/delete); false nếu lỗi nghiệp vụ đã được báo MessageBox
		public static async Task<bool> TryRunAsync(Action action)
		{
			string? error = await Task.Run(() => Execute(action));
			if (error == null)
			{
				return true;
			}
			ShowError(error);
			return false;
		}

		// Chạy thao tác trả kết quả (vd. CreateTenant trả Id)
		public static async Task<(bool ok, T value)> TryRunAsync<T>(Func<T> action)
		{
			var result = await Task.Run(() =>
			{
				try
				{
					return (ok: true, value: action(), error: (string?)null);
				}
				catch (Exception ex) when (IsBusinessError(ex))
				{
					return (ok: false, value: default(T)!, error: ex.Message);
				}
			});
			if (result.ok)
			{
				return (true, result.value);
			}
			ShowError(result.error!);
			return (false, default!);
		}

		// Đọc dữ liệu; không tự MessageBox — caller quyết định hiện lỗi hay SetState trên trang
		public static async Task<(bool ok, T? value, string? error)> TryGetAsync<T>(Func<T> action)
		{
			return await Task.Run(() =>
			{
				try
				{
					return (ok: true, value: (T?)action(), error: (string?)null);
				}
				catch (Exception ex) when (IsBusinessError(ex))
				{
					return (ok: false, value: default, error: ex.Message);
				}
			});
		}

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

		// Chỉ coi là lỗi nghiệp vụ mong đợi; lỗi hệ thống khác vẫn throw bình thường
		private static bool IsBusinessError(Exception ex) =>
			ex is ArgumentException
				or ArgumentOutOfRangeException
				or InvalidOperationException
				or UnauthorizedAccessException;
	}
}
