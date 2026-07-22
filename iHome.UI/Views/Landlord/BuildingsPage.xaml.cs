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
	// quản lý nhà trọ + tòa nhà — master-detail: property grid bên trái, building grid bên phải
	public partial class BuildingsPage : Page
	{
		private readonly User _currentUser;
		private readonly IHomeDbContext _db;
		private readonly LandlordPropertyService _propertyService;
		private readonly LandlordBuildingService _buildingService;
		private List<PropertyDto> _properties = new();
		private List<BuildingDto> _buildings = new();

		public BuildingsPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));

			// one shared context for all landlord building services on this page
			_db = new IHomeDbContext();
			// Assign local/page state inside BuildingsPage without altering business rules
			_propertyService = new LandlordPropertyService(_db);
			// Assign local/page state inside BuildingsPage without altering business rules
			_buildingService = new LandlordBuildingService(_db);
			// Subscribe Unloaded to dispose shared DbContext when page leaves tree
			Unloaded += BuildingsPage_Unloaded;

			// Call helper LoadProperties to refresh UI state from BLL data
			LoadProperties();
		}

		private void BuildingsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe Unloaded handler after single cleanup
			Unloaded -= BuildingsPage_Unloaded;
			// Dispose shared IHomeDbContext to release SQL Server connection
			_db.Dispose();
		}

		private PropertyDto? SelectedProperty => dgProperties.SelectedItem as PropertyDto;
		private BuildingDto? SelectedBuilding => dgBuildings.SelectedItem as BuildingDto;

		// tải toàn bộ property của landlord rồi ApplyPropertyFilter (search client-side)
		private void LoadProperties(int? keepSelectedId = null)
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordPropertyService.GetByLandlord queries or saves landlord properties
				_properties = _propertyService.GetByLandlord(_currentUser.Id);
				// Execute UI step inside LoadProperties
				ApplyPropertyFilter(keepSelectedId);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Không thể tải danh sách nhà trọ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void ApplyPropertyFilter(int? keepSelectedId = null)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			string keyword = (txtSearch?.Text ?? string.Empty).Trim();
			// Assign local/page state inside ApplyPropertyFilter without altering business rules
			var filtered = string.IsNullOrEmpty(keyword)
				// Execute UI step inside ApplyPropertyFilter
				? _properties
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				: _properties.Where(p =>
					// Execute UI step inside ApplyPropertyFilter
					p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					// Materialize query to List for repeated binding and filtering
					|| p.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgProperties.ItemsSource = filtered;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (keepSelectedId.HasValue)
			{
				// Restore or set combo selection to match entity id or filter
				dgProperties.SelectedItem = filtered.FirstOrDefault(p => p.Id == keepSelectedId.Value);
			}

			// Restore or set combo selection to match entity id or filter
			if (dgProperties.SelectedItem == null)
			{
				// Call helper ClearBuildings to refresh UI state from BLL data
				ClearBuildings();
			}
		}

		// tải tòa theo property đang chọn
		private void LoadBuildings()
		{
			// Assign local/page state inside LoadBuildings without altering business rules
			var property = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null)
			{
				// Call helper ClearBuildings to refresh UI state from BLL data
				ClearBuildings();
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordBuildingService.GetByProperty queries or saves buildings under a property
				_buildings = _buildingService.GetByProperty(_currentUser.Id, property.Id);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgBuildings.ItemsSource = _buildings;
				// Show or hide panel/border for empty state or role-specific UI
				brdBuildingState.Visibility = _buildings.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				// Guard clause: only continue when UI selection, role, or input is valid
				if (_buildings.Count == 0)
				{
					// Update TextBlock/TextBox caption or read user-entered text from control
					((TextBlock)brdBuildingState.Child).Text = "Chưa có tòa nhà nào. Bấm + Thêm tòa để tạo mới.";
				}
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbBuildingSection.Text = $"Tòa nhà - {property.Name}";
				// Execute UI step inside LoadBuildings
				SetBuildingToolbarEnabled(true);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Call helper ClearBuildings to refresh UI state from BLL data
				ClearBuildings();
			}
		}

		private void ClearBuildings()
		{
			// Assign local/page state inside ClearBuildings without altering business rules
			_buildings = new();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgBuildings.ItemsSource = null;
			// Show or hide panel/border for empty state or role-specific UI
			brdBuildingState.Visibility = Visibility.Visible;
			// Update TextBlock/TextBox caption or read user-entered text from control
			((TextBlock)brdBuildingState.Child).Text = "Chọn một nhà trọ để xem và quản lý tòa nhà.";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbBuildingSection.Text = "Tòa nhà";
			// Execute UI step inside ClearBuildings
			SetBuildingToolbarEnabled(false);
		}

		private void SetBuildingToolbarEnabled(bool enabled)
		{
			// Enable/disable control during loading or when prerequisites missing
			btnAddBuilding.IsEnabled = enabled;
			// Enable/disable control during loading or when prerequisites missing
			btnEditBuilding.IsEnabled = enabled;
		}

		// only treat double-click on a data row as edit; header clicks stay for sort
		private static bool IsRowDoubleClick(MouseButtonEventArgs e)
		{
			// Assign local/page state inside IsRowDoubleClick without altering business rules
			DependencyObject? current = e.OriginalSource as DependencyObject;
			// Assign local/page state inside IsRowDoubleClick without altering business rules
			while (current != null)
			{
				// Guard clause: only continue when UI selection, role, or input is valid
				if (current is DataGridColumnHeader) return false;
				// Guard clause: only continue when UI selection, role, or input is valid
				if (current is DataGridRow) return true;
				// Assign local/page state inside IsRowDoubleClick without altering business rules
				current = VisualTreeHelper.GetParent(current);
			}
			// Exit method early or return value/tuple to caller
			return false;
		}

		// Assign local/page state inside txtSearch_TextChanged without altering business rules
		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyPropertyFilter(SelectedProperty?.Id);

		// Assign local/page state inside dgProperties_SelectionChanged without altering business rules
		private void dgProperties_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadBuildings();

		private void dgProperties_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Ignore grid header double-clicks; only data rows open edit/view
			if (!IsRowDoubleClick(e)) return;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedProperty != null)
			{
				// Execute UI step inside dgProperties_MouseDoubleClick
				btnEditProperty_Click(sender, e);
			}
		}

		private void dgBuildings_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Ignore grid header double-clicks; only data rows open edit/view
			if (!IsRowDoubleClick(e)) return;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedBuilding != null)
			{
				// Execute UI step inside dgBuildings_MouseDoubleClick
				btnEditBuilding_Click(sender, e);
			}
		}

		// Delegate Enter key to the same handler as the primary save/login button
		private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadProperties(SelectedProperty?.Id);

		private void btnAddProperty_Click(object sender, RoutedEventArgs e)
		{
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new PropertyDialog { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() != true || dialog.Result == null) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordPropertyService.Create queries or saves landlord properties
				_propertyService.Create(_currentUser.Id, dialog.Result);
				// Call helper LoadProperties to refresh UI state from BLL data
				LoadProperties();
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã thêm nhà trọ.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditProperty_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnEditProperty_Click without altering business rules
			var selected = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (selected == null)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn nhà trọ cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordPropertyService.GetForm queries or saves landlord properties
				var form = _propertyService.GetForm(_currentUser.Id, selected.Id);
				// Resolve parent Window so modal dialogs center on the app shell
				var dialog = new PropertyDialog(form) { Owner = Window.GetWindow(this) };
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				// LandlordPropertyService.Update queries or saves landlord properties
				_propertyService.Update(_currentUser.Id, dialog.Result);
				// Call helper LoadProperties to refresh UI state from BLL data
				LoadProperties(selected.Id);
				// Execute UI step inside btnEditProperty_Click
				LoadBuildings();
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã cập nhật nhà trọ.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnAddBuilding_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnAddBuilding_Click without altering business rules
			var property = SelectedProperty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null) return;

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordBuildingService.GetManagerOptions queries or saves buildings under a property
				var managers = _buildingService.GetManagerOptions(property.Id);
				// Resolve parent Window so modal dialogs center on the app shell
				var dialog = new BuildingDialog(property.Id, managers) { Owner = Window.GetWindow(this) };
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				// LandlordBuildingService.Create queries or saves buildings under a property
				_buildingService.Create(_currentUser.Id, dialog.Result);
				// Call helper LoadProperties to refresh UI state from BLL data
				LoadProperties(property.Id);
				// Execute UI step inside btnAddBuilding_Click
				LoadBuildings();
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã thêm tòa nhà.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditBuilding_Click(object sender, RoutedEventArgs e)
		{
			// Assign local/page state inside btnEditBuilding_Click without altering business rules
			var property = SelectedProperty;
			// Assign local/page state inside btnEditBuilding_Click without altering business rules
			var building = SelectedBuilding;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null || building == null)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn tòa nhà cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// LandlordBuildingService.GetForm queries or saves buildings under a property
				var form = _buildingService.GetForm(_currentUser.Id, building.Id);
				// LandlordBuildingService.GetManagerOptions queries or saves buildings under a property
				var managers = _buildingService.GetManagerOptions(property.Id);
				// Resolve parent Window so modal dialogs center on the app shell
				var dialog = new BuildingDialog(property.Id, managers, form) { Owner = Window.GetWindow(this) };
				// Show modal dialog and block until user confirms or cancels
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				// LandlordBuildingService.Update queries or saves buildings under a property
				_buildingService.Update(_currentUser.Id, dialog.Result);
				// Execute UI step inside btnEditBuilding_Click
				LoadBuildings();
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã cập nhật tòa nhà.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
