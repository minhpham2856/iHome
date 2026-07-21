using iHome.BLL.DTOs.Landlord;
using System;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	public partial class TenantDialog : Window
	{
		private readonly int _tenantId;
		public TenantFormDto? Result { get; private set; }

		public TenantDialog(TenantFormDto? tenant = null)
		{
			InitializeComponent();
			_tenantId = tenant?.Id ?? 0;
			if (tenant == null)
			{
				dpDateOfBirth.SelectedDate = DateTime.Today.AddYears(-18);
				return;
			}

			lblTitle.Text = "Cập nhật khách thuê";
			txtFullName.Text = tenant.FullName;
			dpDateOfBirth.SelectedDate = tenant.DateOfBirth.ToDateTime(TimeOnly.MinValue);
			txtIdCard.Text = tenant.IdCardNumber;
			txtPhone.Text = tenant.PhoneNumber;
			txtEmail.Text = tenant.Email ?? string.Empty;
			txtAddress.Text = tenant.PermanentAddress ?? string.Empty;
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (!dpDateOfBirth.SelectedDate.HasValue)
			{
				MessageBox.Show("Vui lòng chọn ngày sinh.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (string.IsNullOrWhiteSpace(txtFullName.Text))
			{
				MessageBox.Show("Họ tên không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (string.IsNullOrWhiteSpace(txtIdCard.Text))
			{
				MessageBox.Show("CCCD không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (string.IsNullOrWhiteSpace(txtPhone.Text))
			{
				MessageBox.Show("Số điện thoại không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Result = new TenantFormDto
			{
				Id = _tenantId,
				FullName = txtFullName.Text,
				DateOfBirth = DateOnly.FromDateTime(dpDateOfBirth.SelectedDate.Value),
				IdCardNumber = txtIdCard.Text,
				PhoneNumber = txtPhone.Text,
				Email = txtEmail.Text,
				PermanentAddress = txtAddress.Text
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// key down enter
		private void txtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; dpDateOfBirth.Focus(); }
		}

		private void dpDateOfBirth_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtIdCard.Focus(); }
		}

		private void txtIdCard_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtPhone.Focus(); }
		}

		private void txtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtEmail.Focus(); }
		}

		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtAddress.Focus(); }
		}

		private void txtAddress_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				btnSave_Click(btnSave, new RoutedEventArgs());
			}
		}
	}
}
