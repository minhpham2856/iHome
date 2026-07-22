using System;
using System.Windows;

namespace iHome.UI.Views.Manager
{
	// Manager UI helper: catch business errors, show MessageBox — synchronous calls only (BLL is not async)
	internal static class ManagerUi
	{
		// Show validation warning dialog
		public static void ShowValidation(string message) =>
			MessageBox.Show(message, "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);

		// Show generic business-error dialog
		public static void ShowError(string message) =>
			MessageBox.Show(message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);

		// Run a write action; returns false if a business error was already shown
		public static bool TryRun(Action action)
		{
			// Run delegated action inside try/catch and capture business error message
			string? error = Execute(action);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (error == null)
			{
				// Exit method early or return value/tuple to caller
				return true;
			}
			// Execute UI step inside TryRun
			ShowError(error);
			// Exit method early or return value/tuple to caller
			return false;
		}

		// Run an action that returns a value (e.g. CreateTenant returns Id)
		public static (bool ok, T value) TryRun<T>(Func<T> action)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Exit method early or return value/tuple to caller
				return (true, action());
			}
			// Catch only BLL validation/authorization exceptions as user-facing errors
			catch (Exception ex) when (IsBusinessError(ex))
			{
				// Execute UI step inside static
				ShowError(ex.Message);
				// Exit method early or return value/tuple to caller
				return (false, default!);
			}
		}

		// Load data; caller handles MessageBox / UI state on failure
		public static (bool ok, T? value, string? error) TryGet<T>(Func<T> action)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Exit method early or return value/tuple to caller
				return (true, action(), null);
			}
			// Catch only BLL validation/authorization exceptions as user-facing errors
			catch (Exception ex) when (IsBusinessError(ex))
			{
				// Exit method early or return value/tuple to caller
				return (false, default, ex.Message);
			}
		}

		// Execute action and return error message, or null on success
		private static string? Execute(Action action)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Execute UI step inside Execute
				action();
				// Exit method early or return value/tuple to caller
				return null;
			}
			// Catch only BLL validation/authorization exceptions as user-facing errors
			catch (Exception ex) when (IsBusinessError(ex))
			{
				// Exit method early or return value/tuple to caller
				return ex.Message;
			}
		}

		// Treat common BLL validation/authorization exceptions as user-facing business errors
		private static bool IsBusinessError(Exception ex) =>
			ex is ArgumentException
				or ArgumentOutOfRangeException
				or InvalidOperationException
				or UnauthorizedAccessException;
	}
}
