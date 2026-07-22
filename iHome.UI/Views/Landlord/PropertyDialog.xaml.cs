using iHome.BLL.DTOs.Landlord;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// Add/edit property — returns PropertyFormDto for Create/Update
	public partial class PropertyDialog : Window
	{
		private readonly int _propertyId;
		public PropertyFormDto? Result { get; private set; }

		public PropertyDialog(PropertyFormDto? property = null)
		{
			InitializeComponent();
			_propertyId = property?.Id ?? 0;
			if (property == null) return;

			lbTitle.Text = "Cập nhật nhà trọ";
			txtName.Text = property.Name;
			txtAddress.Text = property.Address;
			txtDescription.Text = property.Description ?? string.Empty;
			chkIsActive.IsChecked = property.IsActive;
		}

		// Require name + address, then build Result
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtName.Text))
			{
				MessageBox.Show("Tên nhà trọ không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (string.IsNullOrWhiteSpace(txtAddress.Text))
			{
				MessageBox.Show("Địa chỉ không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Result = new PropertyFormDto
			{
				Id = _propertyId,
				Name = txtName.Text,
				Address = txtAddress.Text,
				Description = txtDescription.Text,
				IsActive = chkIsActive.IsChecked == true
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter moves focus: name → address → active checkbox
		private void txtName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtAddress.Focus(); }
		}

		private void txtAddress_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; chkIsActive.Focus(); }
		}
	}
}
