using iHome.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
			CboRoom.ItemsSource = rooms.ToList();
			CboMainTenant.ItemsSource = tenants.ToList();
			CboStatus.ItemsSource = new[]
			{
				new StatusOption("Active", "Đang hoạt động"),
				new StatusOption("Expired", "Đã hết hạn"),
				new StatusOption("Terminated", "Đã chấm dứt")
			};

			if (contract == null)
			{
				DtpStart.SelectedDate = DateTime.Today;
				DtpEnd.SelectedDate = DateTime.Today.AddYears(1);
				CboRoom.SelectedIndex = CboRoom.Items.Count > 0 ? 0 : -1;
				CboMainTenant.SelectedIndex = CboMainTenant.Items.Count > 0 ? 0 : -1;
				CboStatus.SelectedIndex = 0;
				return;
			}

			TxtTitle.Text = "Cập nhật hợp đồng";
			CboRoom.SelectedItem = CboRoom.Items.Cast<ManagerLookupOptionDto>().FirstOrDefault(item => item.Id == contract.RoomId);
			CboMainTenant.SelectedItem = CboMainTenant.Items.Cast<ManagerLookupOptionDto>().FirstOrDefault(item => item.Id == contract.MainTenantId);
			CboRoom.IsEnabled = false;
			CboMainTenant.IsEnabled = false;
			DtpStart.SelectedDate = contract.StartDate.ToDateTime(TimeOnly.MinValue);
			DtpEnd.SelectedDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
			TxtRent.Text = contract.MonthlyRent.ToString(CultureInfo.CurrentCulture);
			TxtDeposit.Text = contract.DepositAmount.ToString(CultureInfo.CurrentCulture);
			CboStatus.SelectedItem = CboStatus.Items.Cast<StatusOption>().First(item => item.Code == contract.Status);
			TxtNotes.Text = contract.Notes ?? string.Empty;
		}

		private void RoomChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_isEditing && CboRoom.SelectedItem is ManagerLookupOptionDto room)
			{
				TxtRent.Text = room.SuggestedAmount.ToString(CultureInfo.CurrentCulture);
				TxtDeposit.Text = room.SuggestedAmount.ToString(CultureInfo.CurrentCulture);
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (CboRoom.SelectedItem is not ManagerLookupOptionDto room ||
				CboMainTenant.SelectedItem is not ManagerLookupOptionDto tenant ||
				CboStatus.SelectedItem is not StatusOption status ||
				!DtpStart.SelectedDate.HasValue ||
				!DtpEnd.SelectedDate.HasValue ||
				!decimal.TryParse(TxtRent.Text, out decimal rent) ||
				!decimal.TryParse(TxtDeposit.Text, out decimal deposit))
			{
				MessageBox.Show("Vui lòng nhập đầy đủ và đúng định dạng.", "Dữ liệu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Result = new ManagerContractFormDto
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
			DialogResult = true;
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private sealed record StatusOption(string Code, string Name);
	}
}
