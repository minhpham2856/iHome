using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
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
	// Dialog tạo/sửa hợp đồng: bắt buộc người thuê chính;
	// khách phụ tùy chọn, tối đa theo MaxOccupancy của loại phòng
	public partial class ContractDialog : Window
	{
		private readonly int _contractId;
		private readonly bool _isEditing;
		private readonly List<LookupOptionDto> _tenants;
		private List<TenantPickItem> _coTenantPicks = new();
		private int _maxCoTenants;
		public ContractFormDto? Result { get; private set; }

		// Nạp form tạo mới hoặc sửa hợp đồng
		public ContractDialog(
			IEnumerable<LookupOptionDto> rooms,
			IEnumerable<LookupOptionDto> tenants,
			ContractFormDto? contract = null)
		{
			InitializeComponent();
			_contractId = contract?.Id ?? 0;
			_isEditing = contract != null;
			_tenants = tenants.ToList();
			cbRoom.ItemsSource = rooms.ToList();
			cbMainTenant.ItemsSource = _tenants;
			cbStatus.ItemsSource = new[]
			{
				new StatusOption(ContractStatus.Active, ContractStatus.Active),
				new StatusOption(ContractStatus.Expired, ContractStatus.Expired),
				new StatusOption(ContractStatus.Terminated, ContractStatus.Terminated)
			};

			if (contract == null)
			{
				dpStart.SelectedDate = DateTime.Today;
				dpEnd.SelectedDate = DateTime.Today.AddYears(1);
				cbRoom.SelectedIndex = cbRoom.Items.Count > 0 ? 0 : -1;
				cbMainTenant.SelectedIndex = cbMainTenant.Items.Count > 0 ? 0 : -1;
				cbStatus.SelectedIndex = 0;
				RefreshCoTenantPanel();
				return;
			}

			lbTitle.Text = "Cập nhật hợp đồng";
			cbRoom.SelectedItem = cbRoom.Items.Cast<LookupOptionDto>().FirstOrDefault(item => item.Id == contract.RoomId);
			cbMainTenant.SelectedItem = cbMainTenant.Items.Cast<LookupOptionDto>().FirstOrDefault(item => item.Id == contract.MainTenantId);
			// Sửa: khóa phòng/người chính; không đổi khách phụ tại đây
			cbRoom.IsEnabled = false;
			cbMainTenant.IsEnabled = false;
			brdCoTenantPanel.Visibility = Visibility.Collapsed;
			lbRoomCapacity.Visibility = Visibility.Collapsed;
			dpStart.SelectedDate = contract.StartDate.ToDateTime(TimeOnly.MinValue);
			dpEnd.SelectedDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
			txtRent.Text = contract.MonthlyRent.ToString(CultureInfo.CurrentCulture);
			txtDeposit.Text = contract.DepositAmount.ToString(CultureInfo.CurrentCulture);
			cbStatus.SelectedItem = cbStatus.Items.Cast<StatusOption>().First(item => item.Code == contract.Status);
			txtNotes.Text = contract.Notes ?? string.Empty;
		}

		// Đổi phòng → gợi ý tiền thuê/cọc và làm mới panel khách phụ
		private void RoomChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_isEditing && cbRoom.SelectedItem is LookupOptionDto room)
			{
				txtRent.Text = room.SuggestedAmount.ToString(CultureInfo.CurrentCulture);
				txtDeposit.Text = room.SuggestedAmount.ToString(CultureInfo.CurrentCulture);
				RefreshCoTenantPanel();
			}
		}

		// Đổi người thuê chính → loại khỏi danh sách khách phụ
		private void MainTenantChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_isEditing)
			{
				RefreshCoTenantPanel();
			}
		}

		// Tick/bỏ tick khách phụ → cập nhật trạng thái sức chứa
		private void CoTenantCheckChanged(object sender, RoutedEventArgs e) => UpdateSelectionStatus();

		// MaxOccupancy là trần sức chứa (không bắt buộc đủ số người);
		// khách phụ tối đa = MaxOccupancy - 1 (đã trừ người thuê chính)
		private void RefreshCoTenantPanel()
		{
			if (_isEditing || cbRoom.SelectedItem is not LookupOptionDto room)
			{
				brdCoTenantPanel.Visibility = Visibility.Collapsed;
				lbRoomCapacity.Visibility = Visibility.Collapsed;
				return;
			}

			int maxOccupancy = Math.Max(1, room.MaxOccupancy);
			_maxCoTenants = Math.Max(0, maxOccupancy - 1);
			lbRoomCapacity.Text = maxOccupancy == 1
				? "Phòng đơn — chỉ cần 1 người thuê chính."
				: $"Sức chứa tối đa {maxOccupancy} người — có thể thuê 1 đến {maxOccupancy} khách đứng tên.";
			lbRoomCapacity.Visibility = Visibility.Visible;

			if (_maxCoTenants <= 0)
			{
				brdCoTenantPanel.Visibility = Visibility.Collapsed;
				lstCoTenants.ItemsSource = null;
				_coTenantPicks = new();
				return;
			}

			int? mainTenantId = (cbMainTenant.SelectedItem as LookupOptionDto)?.Id;
			_coTenantPicks = _tenants
				.Where(tenant => tenant.Id != mainTenantId)
				.Select(tenant => new TenantPickItem
				{
					Id = tenant.Id,
					DisplayName = tenant.DisplayName
				})
				.ToList();
			lstCoTenants.ItemsSource = _coTenantPicks;
			lbCoTenantTitle.Text = "Khách đứng tên thêm (không bắt buộc)";
			lbCoTenantGuide.Text = $"Tick tối đa {_maxCoTenants} khách. Tiền thuê tính chung theo hợp đồng.";
			brdCoTenantPanel.Visibility = Visibility.Visible;
			UpdateSelectionStatus();
		}

		// Hiển thị số khách đã chọn so với trần MaxOccupancy
		private void UpdateSelectionStatus()
		{
			int selected = _coTenantPicks.Count(item => item.IsSelected);
			int totalSelected = selected + (cbMainTenant.SelectedItem != null ? 1 : 0);
			int maxTotal = _maxCoTenants + 1;
			bool over = selected > _maxCoTenants;
			lbSelectionStatus.Text = over
				? $"Đã chọn {totalSelected} khách — vượt sức chứa tối đa {maxTotal}."
				: $"Đã chọn {totalSelected}/{maxTotal} khách đứng tên (tối thiểu 1).";
			lbSelectionStatus.Foreground = over
				? (Brush)FindResource("ColorDanger")
				: (Brush)FindResource("ColorSuccess");
		}

		// Kiểm tra form và trả Result cho trang gọi
		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (cbRoom.SelectedItem is not LookupOptionDto room ||
				cbMainTenant.SelectedItem is not LookupOptionDto tenant ||
				cbStatus.SelectedItem is not StatusOption status ||
				!dpStart.SelectedDate.HasValue ||
				!dpEnd.SelectedDate.HasValue ||
				!decimal.TryParse(txtRent.Text, out decimal rent) ||
				!decimal.TryParse(txtDeposit.Text, out decimal deposit))
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
				// Không vượt trần sức chứa còn lại sau người thuê chính
				if (coTenantIds.Count > _maxCoTenants)
				{
					ManagerUi.ShowValidation(
						$"Chỉ được chọn thêm tối đa {_maxCoTenants} khách (sức chứa còn lại của phòng).");
					return;
				}
			}

			var form = new ContractFormDto
			{
				Id = _contractId,
				RoomId = room.Id,
				MainTenantId = tenant.Id,
				CoTenantIds = coTenantIds,
				StartDate = DateOnly.FromDateTime(dpStart.SelectedDate.Value),
				EndDate = DateOnly.FromDateTime(dpEnd.SelectedDate.Value),
				MonthlyRent = rent,
				DepositAmount = deposit,
				Status = status.Code,
				Notes = txtNotes.Text
			};
			string? error = FormValidation.GetContractError(form, !_isEditing);
			if (error != null)
			{
				ManagerUi.ShowValidation(error);
				return;
			}

			Result = form;
			DialogResult = true;
		}

		// Đóng dialog không lưu
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private sealed record StatusOption(string Code, string Name);

		// Mục tick chọn khách phụ trên danh sách
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

		// Enter → ô người thuê chính
		private void cbRoom_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cbMainTenant.Focus(); }
		}

		// Enter → ngày bắt đầu
		private void cbMainTenant_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; dpStart.Focus(); }
		}

		// Enter → ngày kết thúc
		private void dpStart_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; dpEnd.Focus(); }
		}

		// Enter → tiền thuê
		private void dpEnd_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtRent.Focus(); }
		}

		// Enter → tiền cọc
		private void txtRent_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtDeposit.Focus(); }
		}

		// Enter → trạng thái
		private void txtDeposit_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; cbStatus.Focus(); }
		}

		// Enter → ghi chú
		private void cbStatus_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) { e.Handled = true; txtNotes.Focus(); }
		}

		// Enter trên ghi chú → lưu
		private void txtNotes_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				Save_Click(btnSave, new RoutedEventArgs());
			}
		}
	}
}
