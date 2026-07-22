using iHome.BLL.DTOs.Landlord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// dialog tạo/sửa nhân viên quản lý — chọn nhà trọ + danh sách tòa được phân công (checkbox CanSelect)
	public partial class ManagerDialog : Window
	{
		private readonly int _managerId;
		// callback tải tòa theo property — inject từ ManagersPage để dialog không gọi service trực tiếp
		private readonly Func<int, IEnumerable<int>?, List<ManagerBuildingOptionDto>> _loadBuildings;
		private List<ManagerBuildingOptionDto> _buildings = new();
		// tránh ReloadBuildings khi đang set SelectedValue lúc mở form edit
		private bool _suppressPropertyChange;
		public ManagerFormDto? Result { get; private set; }

		public ManagerDialog(
			IEnumerable<ManagerPropertyOptionDto> properties,
			Func<int, IEnumerable<int>?, List<ManagerBuildingOptionDto>> loadBuildings,
			ManagerFormDto? manager = null)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_loadBuildings = loadBuildings ?? throw new ArgumentNullException(nameof(loadBuildings));
			// Assign local/page state inside ManagerDialog without altering business rules
			_managerId = manager?.Id ?? 0;

			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbProperty.ItemsSource = properties.ToList();

			// Guard clause: only continue when UI selection, role, or input is valid
			if (manager == null)
			{
				// Guard clause: only continue when UI selection, role, or input is valid
				if (cbProperty.Items.Count > 0)
				{
					// Pick default combo index (usually first/all option) after reload
					cbProperty.SelectedIndex = 0;
				}
				// Exit method early or return value/tuple to caller
				return;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật nhân viên";
			// Show or hide panel/border for empty state or role-specific UI
			lbAccountHint.Visibility = Visibility.Collapsed;
			// Show or hide panel/border for empty state or role-specific UI
			chkIsActive.Visibility = Visibility.Visible;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtFullName.Text = manager.FullName;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtEmail.Text = manager.Email;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtPhone.Text = manager.PhoneNumber ?? string.Empty;
			// Read CheckBox to capture boolean flag such as main-tenant selection
			chkIsActive.IsChecked = manager.IsActive;

			// Flip internal flag to suppress duplicate events or mark in-flight operation
			_suppressPropertyChange = true;
			// Assign local/page state inside ManagerDialog without altering business rules
			cbProperty.SelectedValue = manager.PropertyId;
			// Flip internal flag to suppress duplicate events or mark in-flight operation
			_suppressPropertyChange = false;
			// Execute UI step inside ManagerDialog
			ReloadBuildings(manager.BuildingIds);
		}

		// đổi nhà trọ — load lại danh sách tòa (bỏ chọn cũ nếu không thuộc property mới)
		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_suppressPropertyChange || !IsLoaded) return;
			// Execute UI step inside cbProperty_SelectionChanged
			ReloadBuildings(null);
		}

		// gọi callback từ page — selectedIds giữ tick các tòa đang thuộc nhân viên khi edit
		private void ReloadBuildings(IEnumerable<int>? selectedIds)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (cbProperty.SelectedValue is not int propertyId || propertyId <= 0)
			{
				// Work with BLL DTO/form object returned from service or built from controls
				_buildings = new List<ManagerBuildingOptionDto>();
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				lstBuildings.ItemsSource = _buildings;
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside ReloadBuildings without altering business rules
			_buildings = _loadBuildings(propertyId, selectedIds);
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			lstBuildings.ItemsSource = _buildings;
		}

		private void btnSelectAll_Click(object sender, RoutedEventArgs e)
		{
			// chỉ chọn tòa trống / đang thuộc nhân viên này
			foreach (var b in _buildings.Where(x => x.CanSelect)) b.IsSelected = true;
			// Reapply ICollectionView filter after search or combo change
			lstBuildings.Items.Refresh();
		}

		private void btnClear_Click(object sender, RoutedEventArgs e)
		{
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var b in _buildings.Where(x => x.CanSelect)) b.IsSelected = false;
			// Reapply ICollectionView filter after search or combo change
			lstBuildings.Items.Refresh();
		}

		// validate họ tên, email, nhà trọ; thu thập BuildingIds đã tick
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtFullName.Text))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Họ tên không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains('@'))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Email không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Guard clause: only continue when UI selection, role, or input is valid
			if (cbProperty.SelectedValue is not int propertyId || propertyId <= 0)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Chọn nhà trọ để phân công.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside btnSave_Click without altering business rules
			var selectedBuildings = _buildings
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Where(b => b.IsSelected && b.CanSelect)
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Select(b => b.BuildingId)
				// Materialize query to List for repeated binding and filtering
				.ToList();

			// Work with BLL DTO/form object returned from service or built from controls
			Result = new ManagerFormDto
			{
				// Assign local/page state inside btnSave_Click without altering business rules
				Id = _managerId,
				// Update TextBlock/TextBox caption or read user-entered text from control
				FullName = txtFullName.Text,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Email = txtEmail.Text,
				// Update TextBlock/TextBox caption or read user-entered text from control
				PhoneNumber = txtPhone.Text,
				// Read CheckBox to capture boolean flag such as main-tenant selection
				IsActive = chkIsActive.IsChecked == true,
				// Assign local/page state inside btnSave_Click without altering business rules
				PropertyId = propertyId,
				// Assign local/page state inside btnSave_Click without altering business rules
				BuildingIds = selectedBuildings
			};
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter — tab order họ tên → email → phone → combo nhà trọ
		private void txtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtEmail.Focus(); }
		}

		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtPhone.Focus(); }
		}

		private void txtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbProperty.Focus(); }
		}
	}
}
