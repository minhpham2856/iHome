using iHome.BLL.DTOs;
using iHome.BLL.Services.Manager;
using System;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	public partial class TenantDialog : Window
	{
		private readonly int _tenantId;
		public ManagerTenantFormDto? Result { get; private set; }

		public TenantDialog(ManagerTenantFormDto? tenant = null)
		{
			InitializeComponent();
			_tenantId = tenant?.Id ?? 0;
			if (tenant == null)
			{
				DtpDateOfBirth.SelectedDate = DateTime.Today.AddYears(-18);
				return;
			}

			TxtTitle.Text = "Cập nhật khách thuê";
			TxtFullName.Text = tenant.FullName;
			DtpDateOfBirth.SelectedDate = tenant.DateOfBirth.ToDateTime(TimeOnly.MinValue);
			TxtIdCard.Text = tenant.IdCardNumber;
			TxtPhone.Text = tenant.PhoneNumber;
			TxtEmail.Text = tenant.Email ?? string.Empty;
			TxtAddress.Text = tenant.PermanentAddress ?? string.Empty;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (!DtpDateOfBirth.SelectedDate.HasValue)
			{
				ManagerUi.ShowValidation("Vui lòng chọn ngày sinh.");
				return;
			}

			var form = new ManagerTenantFormDto
			{
				Id = _tenantId,
				FullName = TxtFullName.Text,
				DateOfBirth = DateOnly.FromDateTime(DtpDateOfBirth.SelectedDate.Value),
				IdCardNumber = TxtIdCard.Text,
				PhoneNumber = TxtPhone.Text,
				Email = TxtEmail.Text,
				PermanentAddress = TxtAddress.Text
			};
			string? error = ManagerValidation.GetTenantError(form);
			if (error != null)
			{
				ManagerUi.ShowValidation(error);
				return;
			}

			Result = form;
			DialogResult = true;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// key down enter
		private void TxtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; DtpDateOfBirth.Focus(); }
		}

		private void DtpDateOfBirth_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; TxtIdCard.Focus(); }
		}

		private void TxtIdCard_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; TxtPhone.Focus(); }
		}

		private void TxtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; TxtEmail.Focus(); }
		}

		private void TxtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; TxtAddress.Focus(); }
		}

		private void TxtAddress_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				Save_Click(BtnSave, new RoutedEventArgs());
			}
		}
	}
}
