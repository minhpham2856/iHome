using iHome.BLL.DTOs;
using iHome.BLL.Services.Manager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iHome.UI.Views.Manager
{
	public partial class ContractDialog : Window
	{
		private readonly int _contractId;
		private readonly bool _isEditing;
		public ManagerContractFormDto? Result { get; private set; }

		public ContractDialog(
			IEnumerable<ManagerLookupOptionDto> rooms,
			IEnumerable<ManagerLookupOptionDto> tenants,
			ManagerContractFormDto? contract = null)
		{
			InitializeComponent();
			_contractId = contract?.Id ?? 0;
			_isEditing = contract != null;
			CbRoom.ItemsSource = rooms.ToList();
			CbMainTenant.ItemsSource = tenants.ToList();
			CbStatus.ItemsSource = new[]
			{
				new StatusOption("Active", "Đang hoạt động"),
				new StatusOption("Expired", "Đã hết hạn"),
				new StatusOption("Terminated", "Đã chấm dứt")
			};

			if (contract == null)
			{
				DtpStart.SelectedDate = DateTime.Today;
				DtpEnd.SelectedDate = DateTime.Today.AddYears(1);
				CbRoom.SelectedIndex = CbRoom.Items.Count > 0 ? 0 : -1;
				CbMainTenant.SelectedIndex = CbMainTenant.Items.Count > 0 ? 0 : -1;
				CbStatus.SelectedIndex = 0;
				return;
			}

			TxtTitle.Text = "Cập nhật hợp đồng";
			CbRoom.SelectedItem = CbRoom.Items.Cast<ManagerLookupOptionDto>().FirstOrDefault(item => item.Id == contract.RoomId);
			CbMainTenant.SelectedItem = CbMainTenant.Items.Cast<ManagerLookupOptionDto>().FirstOrDefault(item => item.Id == contract.MainTenantId);
			CbRoom.IsEnabled = false;
			CbMainTenant.IsEnabled = false;
			DtpStart.SelectedDate = contract.StartDate.ToDateTime(TimeOnly.MinValue);
			DtpEnd.SelectedDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
			TxtRent.Text = contract.MonthlyRent.ToString(CultureInfo.CurrentCulture);
			TxtDeposit.Text = contract.DepositAmount.ToString(CultureInfo.CurrentCulture);
			CbStatus.SelectedItem = CbStatus.Items.Cast<StatusOption>().First(item => item.Code == contract.Status);
			TxtNotes.Text = contract.Notes ?? string.Empty;
		}

		private void RoomChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_isEditing && CbRoom.SelectedItem is ManagerLookupOptionDto room)
			{
				TxtRent.Text = room.SuggestedAmount.ToString(CultureInfo.CurrentCulture);
				TxtDeposit.Text = room.SuggestedAmount.ToString(CultureInfo.CurrentCulture);
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (CbRoom.SelectedItem is not ManagerLookupOptionDto room ||
				CbMainTenant.SelectedItem is not ManagerLookupOptionDto tenant ||
				CbStatus.SelectedItem is not StatusOption status ||
				!DtpStart.SelectedDate.HasValue ||
				!DtpEnd.SelectedDate.HasValue ||
				!decimal.TryParse(TxtRent.Text, out decimal rent) ||
				!decimal.TryParse(TxtDeposit.Text, out decimal deposit))
			{
				ManagerUi.ShowValidation("Vui lòng nhập đầy đủ và đúng định dạng.");
				return;
			}

			var form = new ManagerContractFormDto
			{
				Id = _contractId,
				RoomId = room.Id,
				MainTenantId = tenant.Id,
				StartDate = DateOnly.FromDateTime(DtpStart.SelectedDate.Value),
				EndDate = DateOnly.FromDateTime(DtpEnd.SelectedDate.Value),
				MonthlyRent = rent,
				DepositAmount = deposit,
				Status = status.Code,
				Notes = TxtNotes.Text
			};
			string? error = ManagerValidation.GetContractError(form, !_isEditing);
			if (error != null)
			{
				ManagerUi.ShowValidation(error);
				return;
			}

			Result = form;
			DialogResult = true;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private sealed record StatusOption(string Code, string Name);

		// key down enter
		private void CbRoom_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; CbMainTenant.Focus(); }
		}

		private void CbMainTenant_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; DtpStart.Focus(); }
		}

		private void DtpStart_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; DtpEnd.Focus(); }
		}

		private void DtpEnd_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; TxtRent.Focus(); }
		}

		private void TxtRent_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; TxtDeposit.Focus(); }
		}

		private void TxtDeposit_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; CbStatus.Focus(); }
		}

		private void CbStatus_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; TxtNotes.Focus(); }
		}

		private void TxtNotes_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				Save_Click(BtnSave, new RoutedEventArgs());
			}
		}
	}
}
