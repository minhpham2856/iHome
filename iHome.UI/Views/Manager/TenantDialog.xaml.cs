using iHome.BLL.DTOs.Manager;
using iHome.BLL.Services.Manager;
using System;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	// Dialog thêm/sửa thông tin khách thuê
	public partial class TenantDialog : Window
	{
		private readonly int _tenantId;
		public TenantFormDto? Result { get; private set; }

		// Nạp form trống hoặc dữ liệu khách cần sửa
		public TenantDialog(TenantFormDto? tenant = null)
		{
			InitializeComponent();
			_tenantId = tenant?.Id ?? 0;
			if (tenant == null)
			{
				dpDateOfBirth.SelectedDate = DateTime.Today.AddYears(-18);
				return;
			}

			lbTitle.Text = "Cập nhật khách thuê";
			txtFullName.Text = tenant.FullName;
			dpDateOfBirth.SelectedDate = tenant.DateOfBirth.ToDateTime(TimeOnly.MinValue);
			txtIdCard.Text = tenant.IdCardNumber;
			txtPhone.Text = tenant.PhoneNumber;
			txtEmail.Text = tenant.Email ?? string.Empty;
			txtAddress.Text = tenant.PermanentAddress ?? string.Empty;
		}

		// Kiểm tra form và trả Result
		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (!dpDateOfBirth.SelectedDate.HasValue)
			{
				ManagerUi.ShowValidation("Vui lòng chọn ngày sinh.");
				return;
			}

			var form = new TenantFormDto
			{
				Id = _tenantId,
				FullName = txtFullName.Text,
				DateOfBirth = DateOnly.FromDateTime(dpDateOfBirth.SelectedDate.Value),
				IdCardNumber = txtIdCard.Text,
				PhoneNumber = txtPhone.Text,
				Email = txtEmail.Text,
				PermanentAddress = txtAddress.Text
			};
			string? error = FormValidation.GetTenantError(form);
			if (error != null)
			{
				ManagerUi.ShowValidation(error);
				return;
			}

			Result = form;
			DialogResult = true;
		}

		// Đóng dialog không lưu
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter → ngày sinh
		private void txtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; dpDateOfBirth.Focus(); }
		}

		// Enter → CCCD
		private void dpDateOfBirth_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtIdCard.Focus(); }
		}

		// Enter → SĐT
		private void txtIdCard_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtPhone.Focus(); }
		}

		// Enter → email
		private void txtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtEmail.Focus(); }
		}

		// Enter → địa chỉ
		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtAddress.Focus(); }
		}

		// Enter trên địa chỉ → lưu
		private void txtAddress_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				Save_Click(btnSave, new RoutedEventArgs());
			}
		}
	}
}
