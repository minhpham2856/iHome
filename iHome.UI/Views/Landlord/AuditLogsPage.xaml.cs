using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace iHome.UI.Views.Landlord
{
	// Read-only audit log page — filter by role/user via LandlordAuditLogService
	public partial class AuditLogsPage : Page
	{
		private readonly User _currentUser;
		private readonly LandlordAuditLogService _service = new();
		// Suppresses FilterChanged while InitFilters / role change rebinds combos
		private bool _isLoadingFilters;

		public AuditLogsPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			Loaded += AuditLogsPage_Loaded;
		}

		// Defer filter bind until XAML is ready (avoids early SelectionChanged)
		private void AuditLogsPage_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= AuditLogsPage_Loaded;
			InitFilters();
			LoadLogs();
		}

		// Load role combo and matching user options
		private void InitFilters()
		{
			_isLoadingFilters = true;
			try
			{
				cbRole.ItemsSource = _service.GetRoleOptions();
				cbRole.SelectedIndex = 0;
				ReloadUserOptions(resetSelection: true);
			}
			finally
			{
				_isLoadingFilters = false;
			}
		}

		// Role filter narrows users; resetSelection=true when role changes
		private void ReloadUserOptions(bool resetSelection)
		{
			string? role = (cbRole.SelectedItem as AuditFilterOptionDto)?.Role;
			var users = _service.GetUserOptions(role);
			cbUser.ItemsSource = users;
			if (resetSelection || cbUser.SelectedItem == null)
			{
				cbUser.SelectedIndex = users.Count > 0 ? 0 : -1;
				return;
			}

			int? keepUserId = (cbUser.SelectedItem as AuditFilterOptionDto)?.Id;
			cbUser.SelectedItem = users.Find(u => u.Id == keepUserId) ?? users[0];
		}

		// Query logs for current landlord + role/user filters
		private void LoadLogs()
		{
			try
			{
				string? role = (cbRole.SelectedItem as AuditFilterOptionDto)?.Role;
				int? userId = (cbUser.SelectedItem as AuditFilterOptionDto)?.Id;
				var logs = _service.GetLogs(_currentUser.Id, role, userId);
				dgLogs.ItemsSource = logs;
				brdState.Visibility = logs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể tải nhật ký", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Role change reloads users; either filter change reloads the grid
		private void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingFilters || !IsLoaded) return;

			if (ReferenceEquals(sender, cbRole))
			{
				_isLoadingFilters = true;
				try
				{
					ReloadUserOptions(resetSelection: true);
				}
				finally
				{
					_isLoadingFilters = false;
				}
			}

			LoadLogs();
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadLogs();
	}
}
