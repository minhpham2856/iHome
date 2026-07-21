using iHome.BLL.DTOs;
using iHome.BLL.Services.Manager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace iHome.UI.Views.Manager
{
	// Dialog tạo/sửa hợp đồng: phòng đơn chỉ cần người thuê chính;
	// phòng đôi/nhiều người bắt buộc tick đủ khách đứng tên theo MaxOccupancy
	public partial class ContractDialog : Window
	{
		private readonly int _contractId;
		private readonly bool _isEditing;
		private readonly List<ManagerLookupOptionDto> _tenants;
		private List<TenantPickItem> _coTenantPicks = new();
		private int _requiredCoTenants;
		public ManagerContractFormDto? Result { get; private set; }

		public ContractDialog(
			IEnumerable<ManagerLookupOptionDto> rooms,
			IEnumerable<ManagerLookupOptionDto> tenants,
			ManagerContractFormDto? contract = null)
		{
			InitializeComponent();
			_contractId = contract?.Id ?? 0;
			_isEditing = contract != null;
			_tenants = tenants.ToList();
			CbRoom.ItemsSource = rooms.ToList();
			CbMainTenant.ItemsSource = _tenants;
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
				RefreshCoTenantPanel();
				return;
			}

			// Sửa hợp đồng: không đổi phòng/khách đứng tên tại đây (dùng "Gán thêm khách" nếu cần)
			TxtTitle.Text = "Cập nhật hợp đồng";
			CbRoom.SelectedItem = CbRoom.Items.Cast<ManagerLookupOptionDto>().FirstOrDefault(item => item.Id == contract.RoomId);
			CbMainTenant.SelectedItem = CbMainTenant.Items.Cast<ManagerLookupOptionDto>().FirstOrDefault(item => item.Id == contract.MainTenantId);
			CbRoom.IsEnabled = false;
			CbMainTenant.IsEnabled = false;
			CoTenantPanel.Visibility = Visibility.Collapsed;
			TxtRoomCapacity.Visibility = Visibility.Collapsed;
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
				RefreshCoTenantPanel();
			}
		}

		private void MainTenantChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_isEditing)
			{
				RefreshCoTenantPanel();
			}
		}

		private void CoTenantCheckChanged(object sender, RoutedEventArgs e) => UpdateSelectionStatus();

		// Hiện checklist khách còn lại + banner sức chứa phòng
		private void RefreshCoTenantPanel()
		{
			if (_isEditing || CbRoom.SelectedItem is not ManagerLookupOptionDto room)
			{
				CoTenantPanel.Visibility = Visibility.Collapsed;
				TxtRoomCapacity.Visibility = Visibility.Collapsed;
				return;
			}

			int maxOccupancy = Math.Max(1, room.MaxOccupancy);
			_requiredCoTenants = maxOccupancy - 1;
			string roomLabel = maxOccupancy == 1
				? "Phòng đơn — chỉ cần 1 người thuê chính."
				: maxOccupancy == 2
					? "Phòng đôi — hợp đồng phải có đủ 2 khách đứng tên (1 chính + 1 còn lại)."
					: $"Phòng {maxOccupancy} người — hợp đồng phải có đủ {maxOccupancy} khách đứng tên.";
			TxtRoomCapacity.Text = roomLabel;
			TxtRoomCapacity.Visibility = Visibility.Visible;

			if (_requiredCoTenants <= 0)
			{
				CoTenantPanel.Visibility = Visibility.Collapsed;
				LstCoTenants.ItemsSource = null;
				_coTenantPicks = new();
				return;
			}

			int? mainTenantId = (CbMainTenant.SelectedItem as ManagerLookupOptionDto)?.Id;
			_coTenantPicks = _tenants
				.Where(tenant => tenant.Id != mainTenantId)
				.Select(tenant => new TenantPickItem
				{
					Id = tenant.Id,
					DisplayName = tenant.DisplayName
				})
				.ToList();
			LstCoTenants.ItemsSource = _coTenantPicks;
			TxtCoTenantTitle.Text = maxOccupancy == 2
				? "Chọn người đứng tên thứ 2"
				: $"Chọn {_requiredCoTenants} khách đứng tên còn lại";
			TxtCoTenantGuide.Text = maxOccupancy == 2
				? "Tick đúng 1 khách bên dưới. Tiền thuê vẫn tính chung 1 hợp đồng, không chia đôi."
				: $"Tick đúng {_requiredCoTenants} khách. Tiền thuê tính chung theo hợp đồng.";
			CoTenantPanel.Visibility = Visibility.Visible;
			UpdateSelectionStatus();
		}

		private void UpdateSelectionStatus()
		{
			int selected = _coTenantPicks.Count(item => item.IsSelected);
			int totalNeeded = _requiredCoTenants + 1;
			int totalSelected = selected + (CbMainTenant.SelectedItem != null ? 1 : 0);
			bool enough = selected == _requiredCoTenants;
			TxtSelectionStatus.Text = enough
				? $"Đã chọn đủ {totalSelected}/{totalNeeded} khách đứng tên — có thể lưu hợp đồng."
				: $"Đã chọn {totalSelected}/{totalNeeded} khách đứng tên — còn thiếu {_requiredCoTenants - selected}.";
			TxtSelectionStatus.Foreground = enough
				? (Brush)FindResource("ColorSuccess")
				: (Brush)FindResource("ColorDanger");
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

			var coTenantIds = new List<int>();
			if (!_isEditing)
			{
				coTenantIds = _coTenantPicks
					.Where(item => item.IsSelected && item.Id != tenant.Id)
					.Select(item => item.Id)
					.Distinct()
					.ToList();
				if (coTenantIds.Count != _requiredCoTenants)
				{
					ManagerUi.ShowValidation(
						_requiredCoTenants == 0
							? "Phòng đơn chỉ cần một người thuê chính."
							: _requiredCoTenants == 1
								? "Phòng đôi cần chọn đúng 1 khách đứng tên thứ 2 (tick vào danh sách)."
								: $"Phòng này cần tick đúng {_requiredCoTenants} khách đứng tên còn lại.");
					return;
				}
			}

			var form = new ManagerContractFormDto
			{
				Id = _contractId,
				RoomId = room.Id,
				MainTenantId = tenant.Id,
				CoTenantIds = coTenantIds,
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

		// Item checkbox trong danh sách khách đứng tên còn lại
		private sealed class TenantPickItem : INotifyPropertyChanged
		{
			private bool _isSelected;
			public int Id { get; set; }
			public string DisplayName { get; set; } = string.Empty;
			public bool IsSelected
			{
				get => _isSelected;
				set
				{
					if (_isSelected == value) return;
					_isSelected = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
				}
			}
			public event PropertyChangedEventHandler? PropertyChanged;
		}

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
