using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace iHome.UI.Views.Landlord
{
	public partial class AuditLogsPage : Page
	{
		private readonly User _currentUser;
		private readonly LandlordAuditLogService _service = new();
		private bool _isLoadingFilters;

		public AuditLogsPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			Loaded += AuditLogsPage_Loaded;
		}

		private void AuditLogsPage_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= AuditLogsPage_Loaded;
			InitFilters();
			LoadLogs();
		}

		private void InitFilters()
		{
			_isLoadingFilters = true;
			try
			{
				cbRole.ItemsSource = _service.GetRoleOptions();
				cbRole.SelectedIndex = 0;
				ReloadUserOptions();
			}
			finally
			{
				_isLoadingFilters = false;
			}
		}

		private void ReloadUserOptions()
		{
			string? role = (cbRole.SelectedItem as AuditFilterOptionDto)?.Role;
			int? keepUserId = (cbUser.SelectedItem as AuditFilterOptionDto)?.Id;
			var users = _service.GetUserOptions(role);
			cbUser.ItemsSource = users;
			cbUser.SelectedItem = users.Find(u => u.Id == keepUserId) ?? users[0];
		}

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

		private void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingFilters || !IsLoaded) return;

			if (ReferenceEquals(sender, cbRole))
			{
				_isLoadingFilters = true;
				try
				{
					ReloadUserOptions();
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
