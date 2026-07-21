using iHome.BLL.DTOs.Landlord;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	public partial class ServiceDialog : Window
	{
		private readonly int _serviceId;
		private readonly int _propertyId;
		public ServiceFormDto? Result { get; private set; }

		public ServiceDialog(
			int propertyId,
			IEnumerable<CalculationMethodOptionDto> methods,
			ServiceFormDto? service = null)
		{
			InitializeComponent();
			_propertyId = propertyId;
			_serviceId = service?.Id ?? 0;

			var methodList = methods.ToList();
			cboCalculationMethod.ItemsSource = methodList;
			cboCalculationMethod.SelectedValue = service?.CalculationMethod ?? "PerRoom";
			if (cboCalculationMethod.SelectedIndex < 0 && methodList.Count > 0)
			{
				cboCalculationMethod.SelectedIndex = 0;
			}

			if (service == null)
			{
				txtUnitPrice.Text = "0";
				return;
			}

			lblTitle.Text = "Cập nhật dịch vụ";
			txtServiceName.Text = service.ServiceName;
			txtUnit.Text = service.Unit;
			txtUnitPrice.Text = service.UnitPrice.ToString(CultureInfo.CurrentCulture);
			chkIsActive.IsChecked = service.IsActive;
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtServiceName.Text))
			{
				MessageBox.Show("Tên dịch vụ không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (string.IsNullOrWhiteSpace(txtUnit.Text))
			{
				MessageBox.Show("Đơn vị tính không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (!decimal.TryParse(txtUnitPrice.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal price) || price < 0)
			{
				MessageBox.Show("Đơn giá không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (cboCalculationMethod.SelectedValue is not string method || string.IsNullOrWhiteSpace(method))
			{
				MessageBox.Show("Vui lòng chọn cách tính.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Result = new ServiceFormDto
			{
				Id = _serviceId,
				PropertyId = _propertyId,
				ServiceName = txtServiceName.Text,
				Unit = txtUnit.Text,
				UnitPrice = price,
				CalculationMethod = method,
				IsActive = chkIsActive.IsChecked == true
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// key down enter
		private void txtServiceName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtUnit.Focus(); }
		}

		private void txtUnit_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtUnitPrice.Focus(); }
		}

		private void txtUnitPrice_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cboCalculationMethod.Focus(); }
		}

		private void cboCalculationMethod_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; btnSave.Focus(); }
		}
	}
}
