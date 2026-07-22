using iHome.BLL.DTOs.Landlord;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Landlord
{
	// dialog thêm/sửa loại dịch vụ thuộc Property — đơn giá, đơn vị, cách tính (PerRoom/PerPerson/…)
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
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside ServiceDialog without altering business rules
			_propertyId = propertyId;
			// Assign local/page state inside ServiceDialog without altering business rules
			_serviceId = service?.Id ?? 0;

			// Materialize query to List for repeated binding and filtering
			var methodList = methods.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbCalculationMethod.ItemsSource = methodList;
			// Assign local/page state inside ServiceDialog without altering business rules
			cbCalculationMethod.SelectedValue = service?.CalculationMethod ?? "PerRoom";
			// Change combo selection to drive filter cascade or dialog default
			if (cbCalculationMethod.SelectedIndex < 0 && methodList.Count > 0)
			{
				// Pick default combo index (usually first/all option) after reload
				cbCalculationMethod.SelectedIndex = 0;
			}

			// Guard clause: only continue when UI selection, role, or input is valid
			if (service == null)
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				txtUnitPrice.Text = "0";
				// Exit method early or return value/tuple to caller
				return;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật dịch vụ";
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtServiceName.Text = service.ServiceName;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtUnit.Text = service.Unit;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtUnitPrice.Text = service.UnitPrice.ToString(CultureInfo.CurrentCulture);
			// Read CheckBox to capture boolean flag such as main-tenant selection
			chkIsActive.IsChecked = service.IsActive;
		}

		// validate tên, đơn vị, giá ≥ 0, cách tính
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtServiceName.Text))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Tên dịch vụ không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (string.IsNullOrWhiteSpace(txtUnit.Text))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đơn vị tính không được để trống.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (!decimal.TryParse(txtUnitPrice.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal price) || price < 0)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đơn giá không hợp lệ.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Guard clause: only continue when UI selection, role, or input is valid
			if (cbCalculationMethod.SelectedValue is not string method || string.IsNullOrWhiteSpace(method))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn cách tính.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Work with BLL DTO/form object returned from service or built from controls
			Result = new ServiceFormDto
			{
				// Assign local/page state inside btnSave_Click without altering business rules
				Id = _serviceId,
				// Assign local/page state inside btnSave_Click without altering business rules
				PropertyId = _propertyId,
				// Update TextBlock/TextBox caption or read user-entered text from control
				ServiceName = txtServiceName.Text,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Unit = txtUnit.Text,
				// Assign local/page state inside btnSave_Click without altering business rules
				UnitPrice = price,
				// Assign local/page state inside btnSave_Click without altering business rules
				CalculationMethod = method,
				// Read CheckBox to capture boolean flag such as main-tenant selection
				IsActive = chkIsActive.IsChecked == true
			};
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		// Enter — tab order qua các field
		private void txtServiceName_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtUnit.Focus(); }
		}

		private void txtUnit_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtUnitPrice.Focus(); }
		}

		private void txtUnitPrice_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbCalculationMethod.Focus(); }
		}

		private void cbCalculationMethod_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; btnSave.Focus(); }
		}
	}
}
