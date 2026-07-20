using System;
using System.Threading.Tasks;
using System.Windows;

namespace iHome.UI.Views.Manager
{
	// Báo lỗi trên UI; bắt exception bên trong Task để debugger không dừng tại throw
	internal static class ManagerUi
	{
		public static void ShowValidation(string message) =>
			MessageBox.Show(message, "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);

		public static void ShowError(string message) =>
			MessageBox.Show(message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);

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

		private static bool IsBusinessError(Exception ex) =>
			ex is ArgumentException
				or ArgumentOutOfRangeException
				or InvalidOperationException
				or UnauthorizedAccessException;
	}
}
