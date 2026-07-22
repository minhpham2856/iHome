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
	// quản lý nhân viên (Manager) — CRUD tài khoản + phân công tòa; email mật khẩu tạm khi tạo mới
	public partial class ManagersPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordManagerService _service;
		private List<ManagerDto> _managers = new();

		public ManagersPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			// Create one EF Core context shared by services on this page
			_db = new IHomeDbContext();
			// Assign local/page state inside ManagersPage without altering business rules
			_service = new LandlordManagerService(_db);
			// Subscribe Unloaded to dispose shared DbContext when page leaves tree
			Unloaded += ManagersPage_Unloaded;
			// Execute UI step inside ManagersPage
			LoadManagers();
		}

		private void ManagersPage_Unloaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe Unloaded handler after single cleanup
			Unloaded -= ManagersPage_Unloaded;
			// Dispose shared IHomeDbContext to release SQL Server connection
			_db.Dispose();
		}

		private ManagerDto? SelectedManager => dgManagers.SelectedItem as ManagerDto;

		private void LoadManagers(int? keepId = null)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetByLandlord to load or mutate scoped data
				_managers = _service.GetByLandlord(_currentUser.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgManagers.ItemsSource = _managers;
				// Show or hide panel/border for empty state or role-specific UI
				brdState.Visibility = _managers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				// Guard clause: only continue when UI selection, role, or input is valid
				if (keepId.HasValue)
				{
					// Restore or set combo selection to match entity id or filter
					dgManagers.SelectedItem = _managers.FirstOrDefault(m => m.Id == keepId.Value);
				}
				// Execute UI step inside LoadManagers
				UpdateButtons();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void UpdateButtons()
		{
			// Assign local/page state inside UpdateButtons without altering business rules
			var selected = SelectedManager;
			// Enable/disable control during loading or when prerequisites missing
			btnEdit.IsEnabled = selected != null;
			// Enable/disable control during loading or when prerequisites missing
			btnDelete.IsEnabled = selected != null;
		}

		// Assign local/page state inside btnRefresh_Click without altering business rules
		private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadManagers(SelectedManager?.Id);

		// tạo manager — cần ít nhất một Property active; hiển thị username + kết quả gửi email
		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			// Call page BLL service GetPropertyOptions to load or mutate scoped data
			var properties = _service.GetPropertyOptions(_currentUser.Id);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (properties.Count == 0)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Chưa có nhà trọ đang hoạt động. Hãy tạo nhà trọ trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Construct modal dialog prefilled with lookup lists or edit DTO
			var dialog = new ManagerDialog(
				// Execute UI step inside btnAdd_Click
				properties,
				// Call page BLL service GetBuildingOptions to load or mutate scoped data
				(propertyId, selected) => _service.GetBuildingOptions(_currentUser.Id, propertyId, selected))
			// Resolve parent Window so modal dialogs center on the app shell
			{ Owner = Window.GetWindow(this) };

			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() != true || dialog.Result == null) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service Create to load or mutate scoped data
				var created = _service.Create(_currentUser.Id, dialog.Result);
				// Assign local/page state inside btnAdd_Click without altering business rules
				string message = created.EmailSent
					// Execute UI step inside btnAdd_Click
					? $"Đã tạo nhân viên.\nTài khoản: {created.Username}\nĐã gửi mật khẩu tạm thời tới email."
					// Execute UI step inside btnAdd_Click
					: $"Đã tạo nhân viên.\nTài khoản: {created.Username}\nMật khẩu tạm: {created.TemporaryPassword}\n\nGửi email thất bại: {created.EmailError}\nHãy gửi mật khẩu cho nhân viên thủ công.";
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(message, "Thành công", MessageBoxButton.OK,
					// Execute UI step inside btnAdd_Click
					created.EmailSent ? MessageBoxImage.Information : MessageBoxImage.Warning);
				// Execute UI step inside btnAdd_Click
				LoadManagers(created.UserId);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Assign local/page state inside btnEdit_Click without altering business rules
		private void btnEdit_Click(object sender, RoutedEventArgs e) => OpenEdit();

		// double-click row — mở edit (bỏ qua click header sort)
		private void dgManagers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (FindVisualParent<DataGridColumnHeader>(e.OriginalSource as DependencyObject) != null) return;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedManager != null) OpenEdit();
		}

		private void OpenEdit()
		{
			// Assign local/page state inside OpenEdit without altering business rules
			var selected = SelectedManager;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (selected == null) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetForm to load or mutate scoped data
				var form = _service.GetForm(_currentUser.Id, selected.Id);
				// Guard clause: only continue when UI selection, role, or input is valid
				if (form == null) return;

				// Call page BLL service GetPropertyOptions to load or mutate scoped data
				var properties = _service.GetPropertyOptions(_currentUser.Id);
				// property của manager có thể inactive — chèn vào list để combo vẫn hiển thị
				if (!properties.Any(p => p.Id == form.PropertyId))
				{
					// Work with BLL DTO/form object returned from service or built from controls
					properties.Insert(0, new ManagerPropertyOptionDto
					{
						// Assign local/page state inside OpenEdit without altering business rules
						Id = form.PropertyId,
						// Assign local/page state inside OpenEdit without altering business rules
						Name = selected.PropertyName
					// Execute UI step inside OpenEdit
					});
				}

				// Construct modal dialog prefilled with lookup lists or edit DTO
				var dialog = new ManagerDialog(
					// Execute UI step inside OpenEdit
					properties,
					// Call page BLL service GetBuildingOptions to load or mutate scoped data
					(propertyId, selectedIds) => _service.GetBuildingOptions(
						// Execute UI step inside OpenEdit
						_currentUser.Id,
						// Execute UI step inside OpenEdit
						propertyId,
						// Execute UI step inside OpenEdit
						selectedIds,
						// Execute UI step inside OpenEdit
						form.Id),
					// Execute UI step inside OpenEdit
					form)
				// Resolve parent Window so modal dialogs center on the app shell
				{ Owner = Window.GetWindow(this) };

				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() == true && dialog.Result != null)
				{
					// Call page BLL service Update to load or mutate scoped data
					_service.Update(_currentUser.Id, dialog.Result);
					// Execute UI step inside OpenEdit
					LoadManagers(dialog.Result.Id);
				}
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// xóa vĩnh viễn user Manager — BLL kiểm tra ràng buộc
		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnDelete_Click without altering business rules
			var selected = SelectedManager;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (selected == null) return;

			// Show Vietnamese MessageBox to confirm, warn, or report success/failure
			var confirm = MessageBox.Show(
				// Execute UI step inside btnDelete_Click
				$"Xóa vĩnh viễn nhân viên \"{selected.FullName}\"? Thao tác này không hoàn tác được.",
				// Execute UI step inside btnDelete_Click
				"Xác nhận xóa",
				// Execute UI step inside btnDelete_Click
				MessageBoxButton.YesNo,
				// Execute UI step inside btnDelete_Click
				MessageBoxImage.Warning);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (confirm != MessageBoxResult.Yes) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service Delete to load or mutate scoped data
				_service.Delete(_currentUser.Id, selected.Id);
				// Execute UI step inside btnDelete_Click
				LoadManagers();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Assign local/page state inside dgManagers_SelectionChanged without altering business rules
		private void dgManagers_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

		// duyệt visual tree lên tìm parent T — phân biệt click header vs row
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
