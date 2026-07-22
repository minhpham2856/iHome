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
	// Properties + buildings master-detail (left property grid, right building grid)
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
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));

			// Shared DbContext for both services on this page
			_db = new IHomeDbContext();
			_propertyService = new LandlordPropertyService(_db);
			_buildingService = new LandlordBuildingService(_db);
			Unloaded += BuildingsPage_Unloaded;

			LoadProperties();
		}

		// Dispose shared context when page leaves the tree
		private void BuildingsPage_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= BuildingsPage_Unloaded;
			_db.Dispose();
		}

		private PropertyDto? SelectedProperty => dgProperties.SelectedItem as PropertyDto;
		private BuildingDto? SelectedBuilding => dgBuildings.SelectedItem as BuildingDto;

		// Load landlord properties then apply client-side search filter
		private void LoadProperties(int? keepSelectedId = null)
		{
			try
			{
				_properties = _propertyService.GetByLandlord(_currentUser.Id);
				ApplyPropertyFilter(keepSelectedId);
			}
			catch (Exception)
			{
				MessageBox.Show("Không thể tải danh sách nhà trọ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Filter property grid by search text; clear buildings if nothing selected
		private void ApplyPropertyFilter(int? keepSelectedId = null)
		{
			string keyword = (txtSearch?.Text ?? string.Empty).Trim();
			var filtered = string.IsNullOrEmpty(keyword)
				? _properties
				: _properties.Where(p =>
					p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| p.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

			dgProperties.ItemsSource = filtered;
			if (keepSelectedId.HasValue)
			{
				dgProperties.SelectedItem = filtered.FirstOrDefault(p => p.Id == keepSelectedId.Value);
			}

			if (dgProperties.SelectedItem == null)
			{
				ClearBuildings();
			}
		}

		// Load buildings for the selected property
		private void LoadBuildings()
		{
			var property = SelectedProperty;
			if (property == null)
			{
				ClearBuildings();
				return;
			}

			try
			{
				_buildings = _buildingService.GetByProperty(_currentUser.Id, property.Id);
				dgBuildings.ItemsSource = _buildings;
				brdBuildingState.Visibility = _buildings.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
				if (_buildings.Count == 0)
				{
					((TextBlock)brdBuildingState.Child).Text = "Chưa có tòa nhà nào. Bấm + Thêm tòa để tạo mới.";
				}
				lbBuildingSection.Text = $"Tòa nhà - {property.Name}";
				SetBuildingToolbarEnabled(true);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
				ClearBuildings();
			}
		}

		// Reset building panel to empty/prompt state
		private void ClearBuildings()
		{
			_buildings = new();
			dgBuildings.ItemsSource = null;
			brdBuildingState.Visibility = Visibility.Visible;
			((TextBlock)brdBuildingState.Child).Text = "Chọn một nhà trọ để xem và quản lý tòa nhà.";
			lbBuildingSection.Text = "Tòa nhà";
			SetBuildingToolbarEnabled(false);
		}

		private void SetBuildingToolbarEnabled(bool enabled)
		{
			btnAddBuilding.IsEnabled = enabled;
			btnEditBuilding.IsEnabled = enabled;
		}

		// Only data-row double-clicks count as edit (headers stay for sort)
		private static bool IsRowDoubleClick(MouseButtonEventArgs e)
		{
			DependencyObject? current = e.OriginalSource as DependencyObject;
			while (current != null)
			{
				if (current is DataGridColumnHeader) return false;
				if (current is DataGridRow) return true;
				current = VisualTreeHelper.GetParent(current);
			}
			return false;
		}

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyPropertyFilter(SelectedProperty?.Id);

		private void dgProperties_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadBuildings();

		private void dgProperties_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!IsRowDoubleClick(e)) return;
			if (SelectedProperty != null)
			{
				btnEditProperty_Click(sender, e);
			}
		}

		private void dgBuildings_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!IsRowDoubleClick(e)) return;
			if (SelectedBuilding != null)
			{
				btnEditBuilding_Click(sender, e);
			}
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadProperties(SelectedProperty?.Id);

		private void btnAddProperty_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new PropertyDialog { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() != true || dialog.Result == null) return;

			try
			{
				_propertyService.Create(_currentUser.Id, dialog.Result);
				LoadProperties();
				MessageBox.Show("Đã thêm nhà trọ.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditProperty_Click(object sender, RoutedEventArgs e)
		{
			var selected = SelectedProperty;
			if (selected == null)
			{
				MessageBox.Show("Vui lòng chọn nhà trọ cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try
			{
				var form = _propertyService.GetForm(_currentUser.Id, selected.Id);
				var dialog = new PropertyDialog(form) { Owner = Window.GetWindow(this) };
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				_propertyService.Update(_currentUser.Id, dialog.Result);
				LoadProperties(selected.Id);
				LoadBuildings();
				MessageBox.Show("Đã cập nhật nhà trọ.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnAddBuilding_Click(object sender, RoutedEventArgs e)
		{
			var property = SelectedProperty;
			if (property == null) return;

			try
			{
				var managers = _buildingService.GetManagerOptions(_currentUser.Id);
				var dialog = new BuildingDialog(property.Id, managers) { Owner = Window.GetWindow(this) };
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				_buildingService.Create(_currentUser.Id, dialog.Result);
				LoadProperties(property.Id);
				LoadBuildings();
				MessageBox.Show("Đã thêm tòa nhà.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void btnEditBuilding_Click(object sender, RoutedEventArgs e)
		{
			var property = SelectedProperty;
			var building = SelectedBuilding;
			if (property == null || building == null)
			{
				MessageBox.Show("Vui lòng chọn tòa nhà cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try
			{
				var form = _buildingService.GetForm(_currentUser.Id, building.Id);
				var managers = _buildingService.GetManagerOptions(_currentUser.Id);
				var dialog = new BuildingDialog(property.Id, managers, form) { Owner = Window.GetWindow(this) };
				if (dialog.ShowDialog() != true || dialog.Result == null) return;

				_buildingService.Update(_currentUser.Id, dialog.Result);
				LoadBuildings();
				MessageBox.Show("Đã cập nhật tòa nhà.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
