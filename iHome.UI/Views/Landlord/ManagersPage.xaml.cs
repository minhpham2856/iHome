using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace iHome.UI.Views.Landlord
{
	// Manager staff CRUD + building assignment; emails temp password on create
	public partial class ManagersPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordManagerService _service;
		private List<ManagerDto> _managers = new();

		public ManagersPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			_db = new IHomeDbContext();
			_service = new LandlordManagerService(_db);
			Unloaded += ManagersPage_Unloaded;
			LoadManagers();
		}

		private void ManagersPage_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= ManagersPage_Unloaded;
			_db.Dispose();
		}

		private ManagerDto? SelectedManager => dgManagers.SelectedItem as ManagerDto;

		// Load manager list for this landlord
		private void LoadManagers(int? keepId = null)
		{
			try
			{
				_managers = _service.GetByLandlord(_currentUser.Id);
				dgManagers.ItemsSource = _managers;
				brdState.Visibility = _managers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				if (keepId.HasValue)
				{
					dgManagers.SelectedItem = _managers.FirstOrDefault(m => m.Id == keepId.Value);
				}
				UpdateButtons();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void UpdateButtons()
		{
			var selected = SelectedManager;
			btnEdit.IsEnabled = selected != null;
			btnDelete.IsEnabled = selected != null;
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadManagers(SelectedManager?.Id);

		// Create manager — needs an active property; show username + email result
		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			var properties = _service.GetPropertyOptions(_currentUser.Id);
			if (properties.Count == 0)
			{
				MessageBox.Show("Chưa có nhà trọ đang hoạt động. Hãy tạo nhà trọ trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var dialog = new ManagerDialog(
				properties,
				(propertyId, selected) => _service.GetBuildingOptions(_currentUser.Id, propertyId, selected))
			{ Owner = Window.GetWindow(this) };

			if (dialog.ShowDialog() != true || dialog.Result == null) return;

			try
			{
				var created = _service.Create(_currentUser.Id, dialog.Result);
				string message = created.EmailSent
					? $"Đã tạo nhân viên.\nTài khoản: {created.Username}\nĐã gửi mật khẩu tạm thời tới email."
					: $"Đã tạo nhân viên.\nTài khoản: {created.Username}\nMật khẩu tạm: {created.TemporaryPassword}\n\nGửi email thất bại: {created.EmailError}\nHãy gửi mật khẩu cho nhân viên thủ công.";
				MessageBox.Show(message, "Thành công", MessageBoxButton.OK,
					created.EmailSent ? MessageBoxImage.Information : MessageBoxImage.Warning);
				LoadManagers(created.UserId);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

		// Row double-click opens edit (ignore header sort clicks)
		private void dgManagers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (FindVisualParent<DataGridColumnHeader>(e.OriginalSource as DependencyObject) != null) return;
			if (SelectedManager != null) OpenEdit();
		}

		private void OpenEdit()
		{
			var selected = SelectedManager;
			if (selected == null) return;

			try
			{
				var form = _service.GetForm(_currentUser.Id, selected.Id);
				if (form == null) return;

				var properties = _service.GetPropertyOptions(_currentUser.Id);
				// Manager property may be inactive — insert so combo still shows it
				if (!properties.Any(p => p.Id == form.PropertyId))
				{
					properties.Insert(0, new ManagerPropertyOptionDto
					{
						Id = form.PropertyId,
						Name = selected.PropertyName
					});
				}

				var dialog = new ManagerDialog(
					properties,
					(propertyId, selectedIds) => _service.GetBuildingOptions(
						_currentUser.Id,
						propertyId,
						selectedIds,
						form.Id),
					form)
				{ Owner = Window.GetWindow(this) };

				if (dialog.ShowDialog() == true && dialog.Result != null)
				{
					_service.Update(_currentUser.Id, dialog.Result);
					LoadManagers(dialog.Result.Id);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Permanently delete Manager user — BLL checks constraints
		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			var selected = SelectedManager;
			if (selected == null) return;

			var confirm = MessageBox.Show(
				$"Xóa vĩnh viễn nhân viên \"{selected.FullName}\"? Thao tác này không hoàn tác được.",
				"Xác nhận xóa",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);
			if (confirm != MessageBoxResult.Yes) return;

			try
			{
				_service.Delete(_currentUser.Id, selected.Id);
				LoadManagers();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void dgManagers_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

		// Walk visual tree for parent T — distinguish header vs row clicks
		private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
		{
			while (child != null)
			{
				if (child is T parent) return parent;
				child = VisualTreeHelper.GetParent(child);
			}
			return null;
		}
	}
}
