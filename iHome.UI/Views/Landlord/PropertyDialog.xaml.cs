using iHome.BLL.DTOs.Landlord;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// dialog thêm/sửa nhà trọ (Property) — trả PropertyFormDto cho LandlordPropertyService Create/Update
	public partial class PropertyDialog : Window
	{
		private readonly int _propertyId;
		// DTO kết quả khi DialogResult=true — BuildingsPage đọc sau ShowDialog
		public PropertyFormDto? Result { get; private set; }

		public PropertyDialog(PropertyFormDto? property = null)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside PropertyDialog without altering business rules
			_propertyId = property?.Id ?? 0;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (property == null) return;

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật nhà trọ";
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtName.Text = property.Name;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtAddress.Text = property.Address;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtDescription.Text = property.Description ?? string.Empty;
			// Read CheckBox to capture boolean flag such as main-tenant selection
			chkIsActive.IsChecked = property.IsActive;
		}

		// validate tên + địa chỉ bắt buộc rồi đóng dialog thành công
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtName.Text))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Tên nhà trọ không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtAddress.Text))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Địa chỉ không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Work with BLL DTO/form object returned from service or built from controls
			Result = new PropertyFormDto
			{
				// Assign local/page state inside btnSave_Click without altering business rules
				Id = _propertyId,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Name = txtName.Text,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Address = txtAddress.Text,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Description = txtDescription.Text,
				// Read CheckBox to capture boolean flag such as main-tenant selection
				IsActive = chkIsActive.IsChecked == true
			};
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter — tab order: tên → địa chỉ → checkbox hoạt động
		private void txtName_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtAddress.Focus(); }
		}

		private void txtAddress_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; chkIsActive.Focus(); }
		}
	}
}
