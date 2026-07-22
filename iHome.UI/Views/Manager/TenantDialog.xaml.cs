using iHome.BLL.DTOs.Manager;
using iHome.BLL.Services.Manager;
using System;
using System.Windows;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	// Code-behind for the TenantDialog modal dialog.
	public partial class TenantDialog : Window
	{
		private readonly int _tenantId;
		public TenantFormDto? Result { get; private set; }

		public TenantDialog(TenantFormDto? tenant = null)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside TenantDialog without altering business rules
			_tenantId = tenant?.Id ?? 0;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (tenant == null)
			{
				// Read/write DatePicker for contract, invoice, or birth date fields
				dpDateOfBirth.SelectedDate = DateTime.Today.AddYears(-18);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật khách thuê";
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtFullName.Text = tenant.FullName;
			// Read/write DatePicker for contract, invoice, or birth date fields
			dpDateOfBirth.SelectedDate = tenant.DateOfBirth.ToDateTime(TimeOnly.MinValue);
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtIdCard.Text = tenant.IdCardNumber;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtPhone.Text = tenant.PhoneNumber;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtEmail.Text = tenant.Email ?? string.Empty;
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtAddress.Text = tenant.PermanentAddress ?? string.Empty;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			// Read/write DatePicker for contract, invoice, or birth date fields
			if (!dpDateOfBirth.SelectedDate.HasValue)
			{
				// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowValidation("Vui lòng chọn ngày sinh.");
				// Exit method early or return value/tuple to caller
				return;
			}

			// Work with BLL DTO/form object returned from service or built from controls
			var form = new TenantFormDto
			{
				// Assign local/page state inside Save_Click without altering business rules
				Id = _tenantId,
				// Update TextBlock/TextBox caption or read user-entered text from control
				FullName = txtFullName.Text,
				// Read/write DatePicker for contract, invoice, or birth date fields
				DateOfBirth = DateOnly.FromDateTime(dpDateOfBirth.SelectedDate.Value),
				// Update TextBlock/TextBox caption or read user-entered text from control
				IdCardNumber = txtIdCard.Text,
				// Update TextBlock/TextBox caption or read user-entered text from control
				PhoneNumber = txtPhone.Text,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Email = txtEmail.Text,
				// Update TextBlock/TextBox caption or read user-entered text from control
				PermanentAddress = txtAddress.Text
			};
			// FormValidation.GetTenantError validates dialog DTO before accepting save
			string? error = FormValidation.GetTenantError(form);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (error != null)
			{
				// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowValidation(error);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside Save_Click without altering business rules
			Result = form;
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private void txtFullName_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; dpDateOfBirth.Focus(); }
		}

		private void dpDateOfBirth_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtIdCard.Focus(); }
		}

		private void txtIdCard_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtPhone.Focus(); }
		}

		private void txtPhone_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtEmail.Focus(); }
		}

		private void txtEmail_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtAddress.Focus(); }
		}

		private void txtAddress_KeyDown(object sender, KeyEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (e.Key == Key.Enter)
			{
				// Consume Enter key so WPF does not trigger default button twice
				e.Handled = true;
				// Handle Enter key to move focus or submit like clicking the primary button
				Save_Click(btnSave, new RoutedEventArgs());
			}
		}
	}
}
