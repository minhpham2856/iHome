using iHome.BLL.DTOs.Landlord;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// dialog thêm/sửa tòa nhà thuộc một Property — gán Manager tùy chọn từ danh sách nhân viên của nhà trọ
	public partial class BuildingDialog : Window
	{
		private readonly int _buildingId;
		private readonly int _propertyId;
		public BuildingFormDto? Result { get; private set; }

		public BuildingDialog(int propertyId, List<ManagerOptionDto> managers, BuildingFormDto? building = null)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside BuildingDialog without altering business rules
			_propertyId = propertyId;
			// Assign local/page state inside BuildingDialog without altering business rules
			_buildingId = building?.Id ?? 0;
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbManager.ItemsSource = managers;

			// Guard clause: only continue when UI selection, role, or input is valid
			if (building == null)
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				txtFloors.Text = "1";
				// Pick default combo index (usually first/all option) after reload
				cbManager.SelectedIndex = 0;
				// Exit method early or return value/tuple to caller
				return;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật tòa nhà";
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtName.Text = building.Name;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtFloors.Text = building.NumberOfFloors.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtDescription.Text = building.Description ?? string.Empty;
			// Read CheckBox to capture boolean flag such as main-tenant selection
			chkIsActive.IsChecked = building.IsActive;
			// Assign local/page state inside BuildingDialog without altering business rules
			cbManager.SelectedValue = building.ManagerId;
			// manager cũ có thể không còn trong list — fallback index 0 (thường là "Không phân công")
			if (cbManager.SelectedItem == null)
			{
				// Pick default combo index (usually first/all option) after reload
				cbManager.SelectedIndex = 0;
			}
		}

		// validate tên và số tầng ≥ 1
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtName.Text))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Tên tòa nhà không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (!int.TryParse(txtFloors.Text.Trim(), out int floors) || floors < 1)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Số tầng phải lớn hơn hoặc bằng 1.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Work with BLL DTO/form object returned from service or built from controls
			Result = new BuildingFormDto
			{
				// Assign local/page state inside btnSave_Click without altering business rules
				Id = _buildingId,
				// Assign local/page state inside btnSave_Click without altering business rules
				PropertyId = _propertyId,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Name = txtName.Text,
				// Assign local/page state inside btnSave_Click without altering business rules
				NumberOfFloors = floors,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Description = txtDescription.Text,
				// Change combo selection to drive filter cascade or dialog default
				ManagerId = (cbManager.SelectedItem as ManagerOptionDto)?.Id,
				// Read CheckBox to capture boolean flag such as main-tenant selection
				IsActive = chkIsActive.IsChecked == true
			};
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter — tab order qua các field chính
		private void txtName_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtFloors.Focus(); }
		}

		private void txtFloors_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbManager.Focus(); }
		}

		private void cbManager_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; chkIsActive.Focus(); }
		}
	}
}
