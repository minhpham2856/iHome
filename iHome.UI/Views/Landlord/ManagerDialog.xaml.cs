using iHome.BLL.DTOs.Landlord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// Create/edit manager — property plus assigned buildings (CanSelect checkboxes)
	public partial class ManagerDialog : Window
	{
		private readonly int _managerId;
		// Loads buildings for a property — injected from ManagersPage so dialog stays UI-only
		private readonly Func<int, IEnumerable<int>?, List<ManagerBuildingOptionDto>> _loadBuildings;
		private List<ManagerBuildingOptionDto> _buildings = new();
		// Suppresses ReloadBuildings while edit form sets SelectedValue
		private bool _suppressPropertyChange;
		public ManagerFormDto? Result { get; private set; }

		public ManagerDialog(
			IEnumerable<ManagerPropertyOptionDto> properties,
			Func<int, IEnumerable<int>?, List<ManagerBuildingOptionDto>> loadBuildings,
			ManagerFormDto? manager = null)
		{
			InitializeComponent();
			_loadBuildings = loadBuildings ?? throw new ArgumentNullException(nameof(loadBuildings));
			_managerId = manager?.Id ?? 0;

			cbProperty.ItemsSource = properties.ToList();

			if (manager == null)
			{
				if (cbProperty.Items.Count > 0)
				{
					cbProperty.SelectedIndex = 0;
				}
				return;
			}

			lbTitle.Text = "Cập nhật nhân viên";
			lbAccountHint.Visibility = Visibility.Collapsed;
			chkIsActive.Visibility = Visibility.Visible;
			txtFullName.Text = manager.FullName;
			txtEmail.Text = manager.Email;
			txtPhone.Text = manager.PhoneNumber ?? string.Empty;
			chkIsActive.IsChecked = manager.IsActive;

			_suppressPropertyChange = true;
			cbProperty.SelectedValue = manager.PropertyId;
			_suppressPropertyChange = false;
			ReloadBuildings(manager.BuildingIds);
		}

		// Property change reloads buildings (clears ticks not in the new property)
		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressPropertyChange || !IsLoaded) return;
			ReloadBuildings(null);
		}

		// Page callback — selectedIds keeps current assignments ticked when editing
		private void ReloadBuildings(IEnumerable<int>? selectedIds)
		{
			if (cbProperty.SelectedValue is not int propertyId || propertyId <= 0)
			{
				_buildings = new List<ManagerBuildingOptionDto>();
				lstBuildings.ItemsSource = _buildings;
				return;
			}

			_buildings = _loadBuildings(propertyId, selectedIds);
			lstBuildings.ItemsSource = _buildings;
		}

		private void btnSelectAll_Click(object sender, RoutedEventArgs e)
		{
			// Only free buildings or ones already assigned to this manager
			foreach (var b in _buildings.Where(x => x.CanSelect)) b.IsSelected = true;
			lstBuildings.Items.Refresh();
		}

		private void btnClear_Click(object sender, RoutedEventArgs e)
		{
			foreach (var b in _buildings.Where(x => x.CanSelect)) b.IsSelected = false;
			lstBuildings.Items.Refresh();
		}

		// Validate name, email, property; collect ticked BuildingIds
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtFullName.Text))
			{
				MessageBox.Show("Họ tên không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains('@'))
			{
				MessageBox.Show("Email không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (cbProperty.SelectedValue is not int propertyId || propertyId <= 0)
			{
				MessageBox.Show("Chọn nhà trọ để phân công.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var selectedBuildings = _buildings
				.Where(b => b.IsSelected && b.CanSelect)
				.Select(b => b.BuildingId)
				.ToList();

			Result = new ManagerFormDto
			{
				Id = _managerId,
				FullName = txtFullName.Text,
				Email = txtEmail.Text,
				PhoneNumber = txtPhone.Text,
				IsActive = chkIsActive.IsChecked == true,
				PropertyId = propertyId,
				BuildingIds = selectedBuildings
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter moves focus: name → email → phone → property combo
		private void txtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtEmail.Focus(); }
		}

		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtPhone.Focus(); }
		}

		private void txtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cbProperty.Focus(); }
		}
	}
}
