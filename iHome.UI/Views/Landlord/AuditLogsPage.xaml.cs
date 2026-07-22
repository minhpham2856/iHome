using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace iHome.UI.Views.Landlord
{
	// trang nhật ký audit — lọc theo vai trò và user; chỉ đọc, dữ liệu từ LandlordAuditLogService
	public partial class AuditLogsPage : Page
	{
		private readonly User _currentUser;
		private readonly LandlordAuditLogService _service = new();
		// cờ tránh FilterChanged khi đang bind combo lúc InitFilters / đổi role
		private bool _isLoadingFilters;

		public AuditLogsPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += AuditLogsPage_Loaded;
		}

		// chờ Page load xong XAML rồi mới bind filter — tránh event sớm
		private void AuditLogsPage_Loaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe one-shot Loaded handler after initialization
			Loaded -= AuditLogsPage_Loaded;
			// Execute UI step inside AuditLogsPage_Loaded
			InitFilters();
			// Call helper LoadLogs to refresh UI state from BLL data
			LoadLogs();
		}

		// nạp combo vai trò và user tương ứng
		private void InitFilters()
		{
			// Flip internal flag to suppress duplicate events or mark in-flight operation
			_isLoadingFilters = true;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetRoleOptions to load or mutate scoped data
				cbRole.ItemsSource = _service.GetRoleOptions();
				// Pick default combo index (usually first/all option) after reload
				cbRole.SelectedIndex = 0;
				// Execute UI step inside InitFilters
				ReloadUserOptions(resetSelection: true);
			}
			// Always reset loading flags and re-enable refresh regardless of success
			finally
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoadingFilters = false;
			}
		}

		// đổi role filter → danh sách user thu hẹp; resetSelection=true khi đổi role
		private void ReloadUserOptions(bool resetSelection)
		{
			// Change combo selection to drive filter cascade or dialog default
			string? role = (cbRole.SelectedItem as AuditFilterOptionDto)?.Role;
			// Call page BLL service GetUserOptions to load or mutate scoped data
			var users = _service.GetUserOptions(role);
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbUser.ItemsSource = users;
			// Restore or set combo selection to match entity id or filter
			if (resetSelection || cbUser.SelectedItem == null)
			{
				// Pick default combo index (usually first/all option) after reload
				cbUser.SelectedIndex = users.Count > 0 ? 0 : -1;
				// Exit method early or return value/tuple to caller
				return;
			}

			// Change combo selection to drive filter cascade or dialog default
			int? keepUserId = (cbUser.SelectedItem as AuditFilterOptionDto)?.Id;
			// Restore or set combo selection to match entity id or filter
			cbUser.SelectedItem = users.Find(u => u.Id == keepUserId) ?? users[0];
		}

		// query log theo landlord hiện tại + filter role/user
		private void LoadLogs()
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Change combo selection to drive filter cascade or dialog default
				string? role = (cbRole.SelectedItem as AuditFilterOptionDto)?.Role;
				// Change combo selection to drive filter cascade or dialog default
				int? userId = (cbUser.SelectedItem as AuditFilterOptionDto)?.Id;
				// Call page BLL service GetLogs to load or mutate scoped data
				var logs = _service.GetLogs(_currentUser.Id, role, userId);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgLogs.ItemsSource = logs;
				// Show or hide panel/border for empty state or role-specific UI
				brdState.Visibility = logs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể tải nhật ký", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// cbRole hoặc cbUser đổi — reload user list nếu là role, luôn reload grid
		private void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isLoadingFilters || !IsLoaded) return;

			// Guard clause: only continue when UI selection, role, or input is valid
			if (ReferenceEquals(sender, cbRole))
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoadingFilters = true;
				// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
				try
				{
					// lọc người dùng theo vai trò đã chọn
					ReloadUserOptions(resetSelection: true);
				}
				// Always reset loading flags and re-enable refresh regardless of success
				finally
				{
					// Flip internal flag to suppress duplicate events or mark in-flight operation
					_isLoadingFilters = false;
				}
			}

			// Call helper LoadLogs to refresh UI state from BLL data
			LoadLogs();
		}

		// Delegate Enter key to the same handler as the primary save/login button
		private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadLogs();
	}
}
