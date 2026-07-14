using iHome.BLL.DTOs;
using System;
using System.Windows;

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
				MessageBox.Show("Vui lòng chọn ngày sinh.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Result = new ManagerTenantFormDto
			{
				Id = _tenantId,
				FullName = TxtFullName.Text,
				DateOfBirth = DateOnly.FromDateTime(DtpDateOfBirth.SelectedDate.Value),
				IdCardNumber = TxtIdCard.Text,
				PhoneNumber = TxtPhone.Text,
				Email = TxtEmail.Text,
				PermanentAddress = TxtAddress.Text
			};
			DialogResult = true;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
	}
}
