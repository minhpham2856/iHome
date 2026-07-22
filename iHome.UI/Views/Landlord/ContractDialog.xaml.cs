using iHome.BLL.DTOs.Landlord;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace iHome.UI.Views.Landlord
{
	// dialog sửa hợp đồng — chỉ cho phép chỉnh thời hạn, tiền, trạng thái, ghi chú; vị trí/khách chính read-only
	public partial class ContractDialog : Window
	{
		private readonly int _contractId;
		public ContractFormDto? Result { get; private set; }

		public ContractDialog(ContractFormDto form, IReadOnlyList<ContractStatusOptionDto> statuses)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Execute UI step inside ContractDialog
			ArgumentNullException.ThrowIfNull(form);
			// Assign local/page state inside ContractDialog without altering business rules
			_contractId = form.Id;
			// thông tin định danh — không cho sửa trên UI
			lbLocation.Text = $"{form.PropertyName} / {form.BuildingName} / Phòng {form.RoomNumber}";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbMainTenant.Text = form.MainTenantName;
			// Read/write DatePicker for contract, invoice, or birth date fields
			dpStart.SelectedDate = form.StartDate.ToDateTime(TimeOnly.MinValue);
			// Read/write DatePicker for contract, invoice, or birth date fields
			dpEnd.SelectedDate = form.EndDate.ToDateTime(TimeOnly.MinValue);
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtRent.Text = form.MonthlyRent.ToString("0", CultureInfo.InvariantCulture);
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtDeposit.Text = form.DepositAmount.ToString("0", CultureInfo.InvariantCulture);
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtNotes.Text = form.Notes ?? string.Empty;

			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbStatus.ItemsSource = statuses;
			// Restore or set combo selection to match entity id or filter
			cbStatus.SelectedItem = statuses.FirstOrDefault(s =>
				// Execute UI step inside ContractDialog
				string.Equals(s.Value, form.Status, StringComparison.OrdinalIgnoreCase))
				// Execute UI step inside ContractDialog
				?? statuses.FirstOrDefault();
		}

		// parse tiền thuê/cọc (Invariant hoặc locale hiện tại) và validate ngày + trạng thái
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Read/write DatePicker for contract, invoice, or birth date fields
			if (!dpStart.SelectedDate.HasValue || !dpEnd.SelectedDate.HasValue)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn đầy đủ thời hạn hợp đồng.", "Dữ liệu không hợp lệ",
					// Execute UI step inside btnSave_Click
					MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (!decimal.TryParse(txtRent.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rent) &&
				// Update TextBlock/TextBox caption or read user-entered text from control
				!decimal.TryParse(txtRent.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out rent))
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Tiền thuê không hợp lệ.", "Dữ liệu không hợp lệ",
					// Execute UI step inside btnSave_Click
					MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside btnSave_Click without altering business rules
			decimal deposit = 0;
			// Update TextBlock/TextBox caption or read user-entered text from control
			if (!string.IsNullOrWhiteSpace(txtDeposit.Text))
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				if (!decimal.TryParse(txtDeposit.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out deposit) &&
					// Update TextBlock/TextBox caption or read user-entered text from control
					!decimal.TryParse(txtDeposit.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out deposit))
				{
					// Show Vietnamese MessageBox to confirm, warn, or report success/failure
					MessageBox.Show("Tiền cọc không hợp lệ.", "Dữ liệu không hợp lệ",
						// Execute UI step inside btnSave_Click
						MessageBoxButton.OK, MessageBoxImage.Warning);
					// Exit method early or return value/tuple to caller
					return;
				}
			}

			// Change combo selection to drive filter cascade or dialog default
			if (cbStatus.SelectedItem is not ContractStatusOptionDto status)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Vui lòng chọn trạng thái.", "Dữ liệu không hợp lệ",
					// Execute UI step inside btnSave_Click
					MessageBoxButton.OK, MessageBoxImage.Warning);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Work with BLL DTO/form object returned from service or built from controls
			Result = new ContractFormDto
			{
				// Assign local/page state inside btnSave_Click without altering business rules
				Id = _contractId,
				// Read/write DatePicker for contract, invoice, or birth date fields
				StartDate = DateOnly.FromDateTime(dpStart.SelectedDate.Value),
				// Read/write DatePicker for contract, invoice, or birth date fields
				EndDate = DateOnly.FromDateTime(dpEnd.SelectedDate.Value),
				// Assign local/page state inside btnSave_Click without altering business rules
				MonthlyRent = rent,
				// Assign local/page state inside btnSave_Click without altering business rules
				DepositAmount = deposit,
				// Assign local/page state inside btnSave_Click without altering business rules
				Status = status.Value,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Notes = txtNotes.Text
			};
			// Close dialog successfully so caller reads Result/output properties
			DialogResult = true;
		}

		// Cancel dialog without persisting changes
		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
	}
}
