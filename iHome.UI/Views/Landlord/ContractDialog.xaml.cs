using iHome.BLL.DTOs.Landlord;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace iHome.UI.Views.Landlord
{
	// Edit contract — term, rent, status, notes only; location/main tenant are read-only
	public partial class ContractDialog : Window
	{
		private readonly int _contractId;
		public ContractFormDto? Result { get; private set; }

		public ContractDialog(ContractFormDto form, IReadOnlyList<ContractStatusOptionDto> statuses)
		{
			InitializeComponent();
			ArgumentNullException.ThrowIfNull(form);
			_contractId = form.Id;
			// Identity fields — not editable here
			lbLocation.Text = $"{form.PropertyName} / {form.BuildingName} / Phòng {form.RoomNumber}";
			lbMainTenant.Text = form.MainTenantName;
			dpStart.SelectedDate = form.StartDate.ToDateTime(TimeOnly.MinValue);
			dpEnd.SelectedDate = form.EndDate.ToDateTime(TimeOnly.MinValue);
			txtRent.Text = form.MonthlyRent.ToString("0", CultureInfo.InvariantCulture);
			txtDeposit.Text = form.DepositAmount.ToString("0", CultureInfo.InvariantCulture);
			txtNotes.Text = form.Notes ?? string.Empty;

			cbStatus.ItemsSource = statuses;
			cbStatus.SelectedItem = statuses.FirstOrDefault(s =>
				string.Equals(s.Value, form.Status, StringComparison.OrdinalIgnoreCase))
				?? statuses.FirstOrDefault();
		}

		// Parse rent/deposit and validate dates + status
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if (!dpStart.SelectedDate.HasValue || !dpEnd.SelectedDate.HasValue)
			{
				MessageBox.Show("Vui lòng chọn đầy đủ thời hạn hợp đồng.", "Dữ liệu không hợp lệ",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (!decimal.TryParse(txtRent.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rent) &&
				!decimal.TryParse(txtRent.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out rent))
			{
				MessageBox.Show("Tiền thuê không hợp lệ.", "Dữ liệu không hợp lệ",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			decimal deposit = 0;
			if (!string.IsNullOrWhiteSpace(txtDeposit.Text))
			{
				if (!decimal.TryParse(txtDeposit.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out deposit) &&
					!decimal.TryParse(txtDeposit.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out deposit))
				{
					MessageBox.Show("Tiền cọc không hợp lệ.", "Dữ liệu không hợp lệ",
						MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}

			if (cbStatus.SelectedItem is not ContractStatusOptionDto status)
			{
				MessageBox.Show("Vui lòng chọn trạng thái.", "Dữ liệu không hợp lệ",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Result = new ContractFormDto
			{
				Id = _contractId,
				StartDate = DateOnly.FromDateTime(dpStart.SelectedDate.Value),
				EndDate = DateOnly.FromDateTime(dpEnd.SelectedDate.Value),
				MonthlyRent = rent,
				DepositAmount = deposit,
				Status = status.Value,
				Notes = txtNotes.Text
			};
			DialogResult = true;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
	}
}
