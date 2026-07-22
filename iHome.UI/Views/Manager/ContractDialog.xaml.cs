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
	// Dialog tạo/sửa hợp đồng: phòng đơn chỉ cần người thuê chính;
	// phòng đôi/nhiều người bắt buộc tick đủ khách đứng tên theo MaxOccupancy
	public partial class ContractDialog : Window
	{
		private readonly int _contractId;
		private readonly bool _isEditing;
		private readonly List<LookupOptionDto> _tenants;
		private List<TenantPickItem> _coTenantPicks = new();
		private int _requiredCoTenants;
		public ContractFormDto? Result { get; private set; }

		public ContractDialog(
			IEnumerable<LookupOptionDto> rooms,
			IEnumerable<LookupOptionDto> tenants,
			ContractFormDto? contract = null)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside ContractDialog without altering business rules
			_contractId = contract?.Id ?? 0;
			// Assign local/page state inside ContractDialog without altering business rules
			_isEditing = contract != null;
			// Materialize query to List for repeated binding and filtering
			_tenants = tenants.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbRoom.ItemsSource = rooms.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbMainTenant.ItemsSource = _tenants;
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbStatus.ItemsSource = new[]
			{
				// Execute UI step inside ContractDialog
				new StatusOption(ContractStatus.Active, ContractStatus.Active),
				// Execute UI step inside ContractDialog
				new StatusOption(ContractStatus.Expired, ContractStatus.Expired),
				// Execute UI step inside ContractDialog
				new StatusOption(ContractStatus.Terminated, ContractStatus.Terminated)
			};

			// Guard clause: only continue when UI selection, role, or input is valid
			if (contract == null)
			{
				// Read/write DatePicker for contract, invoice, or birth date fields
				dpStart.SelectedDate = DateTime.Today;
				// Read/write DatePicker for contract, invoice, or birth date fields
				dpEnd.SelectedDate = DateTime.Today.AddYears(1);
				// Pick default combo index (usually first/all option) after reload
				cbRoom.SelectedIndex = cbRoom.Items.Count > 0 ? 0 : -1;
				// Pick default combo index (usually first/all option) after reload
				cbMainTenant.SelectedIndex = cbMainTenant.Items.Count > 0 ? 0 : -1;
				// Pick default combo index (usually first/all option) after reload
				cbStatus.SelectedIndex = 0;
				// Execute UI step inside ContractDialog
				RefreshCoTenantPanel();
				// Exit method early or return value/tuple to caller
				return;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật hợp đồng";
			// Restore or set combo selection to match entity id or filter
			cbRoom.SelectedItem = cbRoom.Items.Cast<LookupOptionDto>().FirstOrDefault(item => item.Id == contract.RoomId);
			// Restore or set combo selection to match entity id or filter
			cbMainTenant.SelectedItem = cbMainTenant.Items.Cast<LookupOptionDto>().FirstOrDefault(item => item.Id == contract.MainTenantId);
			// Enable/disable control during loading or when prerequisites missing
			cbRoom.IsEnabled = false;
			// Enable/disable control during loading or when prerequisites missing
			cbMainTenant.IsEnabled = false;
			// Show or hide panel/border for empty state or role-specific UI
			brdCoTenantPanel.Visibility = Visibility.Collapsed;
			// Show or hide panel/border for empty state or role-specific UI
			lbRoomCapacity.Visibility = Visibility.Collapsed;
			// Read/write DatePicker for contract, invoice, or birth date fields
			dpStart.SelectedDate = contract.StartDate.ToDateTime(TimeOnly.MinValue);
			// Read/write DatePicker for contract, invoice, or birth date fields
			dpEnd.SelectedDate = contract.EndDate.ToDateTime(TimeOnly.MinValue);
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtRent.Text = contract.MonthlyRent.ToString(CultureInfo.CurrentCulture);
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtDeposit.Text = contract.DepositAmount.ToString(CultureInfo.CurrentCulture);
			// Restore or set combo selection to match entity id or filter
			cbStatus.SelectedItem = cbStatus.Items.Cast<StatusOption>().First(item => item.Code == contract.Status);
			// Update TextBlock/TextBox caption or read user-entered text from control
			txtNotes.Text = contract.Notes ?? string.Empty;
		}

		private void RoomChanged(object sender, SelectionChangedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (!_isEditing && cbRoom.SelectedItem is LookupOptionDto room)
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				txtRent.Text = room.SuggestedAmount.ToString(CultureInfo.CurrentCulture);
				// Update TextBlock/TextBox caption or read user-entered text from control
				txtDeposit.Text = room.SuggestedAmount.ToString(CultureInfo.CurrentCulture);
				// Execute UI step inside RoomChanged
				RefreshCoTenantPanel();
			}
		}

		private void MainTenantChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!_isEditing)
			{
				// Execute UI step inside MainTenantChanged
				RefreshCoTenantPanel();
			}
		}

		// Assign local/page state inside CoTenantCheckChanged without altering business rules
		private void CoTenantCheckChanged(object sender, RoutedEventArgs e) => UpdateSelectionStatus();

		private void RefreshCoTenantPanel()
		{
			// Change combo selection to drive filter cascade or dialog default
			if (_isEditing || cbRoom.SelectedItem is not LookupOptionDto room)
			{
				// Show or hide panel/border for empty state or role-specific UI
				brdCoTenantPanel.Visibility = Visibility.Collapsed;
				// Show or hide panel/border for empty state or role-specific UI
				lbRoomCapacity.Visibility = Visibility.Collapsed;
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside RefreshCoTenantPanel without altering business rules
			int maxOccupancy = Math.Max(1, room.MaxOccupancy);
			// Assign local/page state inside RefreshCoTenantPanel without altering business rules
			_requiredCoTenants = maxOccupancy - 1;
			// Assign local/page state inside RefreshCoTenantPanel without altering business rules
			string roomLabel = maxOccupancy == 1
				// Execute UI step inside RefreshCoTenantPanel
				? "Phòng đơn — chỉ cần 1 người thuê chính."
				// Assign local/page state inside RefreshCoTenantPanel without altering business rules
				: maxOccupancy == 2
					// Execute UI step inside RefreshCoTenantPanel
					? "Phòng đôi — hợp đồng phải có đủ 2 khách đứng tên (1 chính + 1 còn lại)."
					// Execute UI step inside RefreshCoTenantPanel
					: $"Phòng {maxOccupancy} người — hợp đồng phải có đủ {maxOccupancy} khách đứng tên.";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbRoomCapacity.Text = roomLabel;
			// Show or hide panel/border for empty state or role-specific UI
			lbRoomCapacity.Visibility = Visibility.Visible;

			// Guard clause: only continue when UI selection, role, or input is valid
			if (_requiredCoTenants <= 0)
			{
				// Show or hide panel/border for empty state or role-specific UI
				brdCoTenantPanel.Visibility = Visibility.Collapsed;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				lstCoTenants.ItemsSource = null;
				// Assign local/page state inside RefreshCoTenantPanel without altering business rules
				_coTenantPicks = new();
				// Exit method early or return value/tuple to caller
				return;
			}

			// Change combo selection to drive filter cascade or dialog default
			int? mainTenantId = (cbMainTenant.SelectedItem as LookupOptionDto)?.Id;
			// Assign local/page state inside RefreshCoTenantPanel without altering business rules
			_coTenantPicks = _tenants
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Where(tenant => tenant.Id != mainTenantId)
				// LINQ step to shape in-memory list for filters, KPIs, or binding
				.Select(tenant => new TenantPickItem
				{
					// Assign local/page state inside RefreshCoTenantPanel without altering business rules
					Id = tenant.Id,
					// Assign local/page state inside RefreshCoTenantPanel without altering business rules
					DisplayName = tenant.DisplayName
				// Execute UI step inside RefreshCoTenantPanel
				})
				// Materialize query to List for repeated binding and filtering
				.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			lstCoTenants.ItemsSource = _coTenantPicks;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbCoTenantTitle.Text = maxOccupancy == 2
				// Execute UI step inside RefreshCoTenantPanel
				? "Chọn người đứng tên thứ 2"
				// Execute UI step inside RefreshCoTenantPanel
				: $"Chọn {_requiredCoTenants} khách đứng tên còn lại";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbCoTenantGuide.Text = maxOccupancy == 2
				// Execute UI step inside RefreshCoTenantPanel
				? "Tick đúng 1 khách bên dưới. Tiền thuê vẫn tính chung 1 hợp đồng, không chia đôi."
				// Execute UI step inside RefreshCoTenantPanel
				: $"Tick đúng {_requiredCoTenants} khách. Tiền thuê tính chung theo hợp đồng.";
			// Show or hide panel/border for empty state or role-specific UI
			brdCoTenantPanel.Visibility = Visibility.Visible;
			// Execute UI step inside RefreshCoTenantPanel
			UpdateSelectionStatus();
		}

		private void UpdateSelectionStatus()
		{
			// Aggregate list into KPI number shown on summary labels
			int selected = _coTenantPicks.Count(item => item.IsSelected);
			// Assign local/page state inside UpdateSelectionStatus without altering business rules
			int totalNeeded = _requiredCoTenants + 1;
			// Change combo selection to drive filter cascade or dialog default
			int totalSelected = selected + (cbMainTenant.SelectedItem != null ? 1 : 0);
			// Assign local/page state inside UpdateSelectionStatus without altering business rules
			bool enough = selected == _requiredCoTenants;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbSelectionStatus.Text = enough
				// Execute UI step inside UpdateSelectionStatus
				? $"Đã chọn đủ {totalSelected}/{totalNeeded} khách đứng tên — có thể lưu hợp đồng."
				// Execute UI step inside UpdateSelectionStatus
				: $"Đã chọn {totalSelected}/{totalNeeded} khách đứng tên — còn thiếu {_requiredCoTenants - selected}.";
			// Assign local/page state inside UpdateSelectionStatus without altering business rules
			lbSelectionStatus.Foreground = enough
				// Fetch dynamic resource brush for success/danger label color
				? (Brush)FindResource("ColorSuccess")
				// Fetch dynamic resource brush for success/danger label color
				: (Brush)FindResource("ColorDanger");
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbRoom.SelectedItem is not LookupOptionDto room ||
				// Change combo selection to drive filter cascade or dialog default
				cbMainTenant.SelectedItem is not LookupOptionDto tenant ||
				// Change combo selection to drive filter cascade or dialog default
				cbStatus.SelectedItem is not StatusOption status ||
				// Read/write DatePicker for contract, invoice, or birth date fields
				!dpStart.SelectedDate.HasValue ||
				// Read/write DatePicker for contract, invoice, or birth date fields
				!dpEnd.SelectedDate.HasValue ||
				// Update TextBlock/TextBox caption or read user-entered text from control
				!decimal.TryParse(txtRent.Text, out decimal rent) ||
				// Update TextBlock/TextBox caption or read user-entered text from control
				!decimal.TryParse(txtDeposit.Text, out decimal deposit))
			{
				// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowValidation("Vui lòng nhập đầy đủ và đúng định dạng.");
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside Save_Click without altering business rules
			var coTenantIds = new List<int>();
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!_isEditing)
			{
				// Assign local/page state inside Save_Click without altering business rules
				coTenantIds = _coTenantPicks
					// LINQ step to shape in-memory list for filters, KPIs, or binding
					.Where(item => item.IsSelected && item.Id != tenant.Id)
					// LINQ step to shape in-memory list for filters, KPIs, or binding
					.Select(item => item.Id)
					// LINQ step to shape in-memory list for filters, KPIs, or binding
					.Distinct()
					// Materialize query to List for repeated binding and filtering
					.ToList();
				// Guard clause: only continue when UI selection, role, or input is valid
				if (coTenantIds.Count != _requiredCoTenants)
				{
					// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
					ManagerUi.ShowValidation(
						// Assign local/page state inside Save_Click without altering business rules
						_requiredCoTenants == 0
							// Execute UI step inside Save_Click
							? "Phòng đơn chỉ cần một người thuê chính."
							// Assign local/page state inside Save_Click without altering business rules
							: _requiredCoTenants == 1
								// Execute UI step inside Save_Click
								? "Phòng đôi cần chọn đúng 1 khách đứng tên thứ 2 (tick vào danh sách)."
								// Execute UI step inside Save_Click
								: $"Phòng này cần tick đúng {_requiredCoTenants} khách đứng tên còn lại.");
					// Exit method early or return value/tuple to caller
					return;
				}
			}

			// Work with BLL DTO/form object returned from service or built from controls
			var form = new ContractFormDto
			{
				// Assign local/page state inside Save_Click without altering business rules
				Id = _contractId,
				// Assign local/page state inside Save_Click without altering business rules
				RoomId = room.Id,
				// Assign local/page state inside Save_Click without altering business rules
				MainTenantId = tenant.Id,
				// Assign local/page state inside Save_Click without altering business rules
				CoTenantIds = coTenantIds,
				// Read/write DatePicker for contract, invoice, or birth date fields
				StartDate = DateOnly.FromDateTime(dpStart.SelectedDate.Value),
				// Read/write DatePicker for contract, invoice, or birth date fields
				EndDate = DateOnly.FromDateTime(dpEnd.SelectedDate.Value),
				// Assign local/page state inside Save_Click without altering business rules
				MonthlyRent = rent,
				// Assign local/page state inside Save_Click without altering business rules
				DepositAmount = deposit,
				// Assign local/page state inside Save_Click without altering business rules
				Status = status.Code,
				// Update TextBlock/TextBox caption or read user-entered text from control
				Notes = txtNotes.Text
			};
			// FormValidation.GetContractError validates dialog DTO before accepting save
			string? error = FormValidation.GetContractError(form, !_isEditing);
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

		private sealed record StatusOption(string Code, string Name);

		private sealed class TenantPickItem : INotifyPropertyChanged
		{
			// Execute UI step inside StatusOption
			private bool _isSelected;
			// Execute UI step inside StatusOption
			public int Id { get; set; }
			// Assign local/page state inside StatusOption without altering business rules
			public string DisplayName { get; set; } = string.Empty;
			// Execute UI step inside StatusOption
			public bool IsSelected
			{
				// Assign local/page state inside StatusOption without altering business rules
				get => _isSelected;
				// Execute UI step inside StatusOption
				set
				{
					// Guard clause: only continue when UI selection, role, or input is valid
					if (_isSelected == value) return;
					// Assign local/page state inside StatusOption without altering business rules
					_isSelected = value;
					// Notify WPF binding that co-tenant checkbox selection changed
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
				}
			}
			// Execute UI step inside StatusOption
			public event PropertyChangedEventHandler? PropertyChanged;
		}

		private void cbRoom_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbMainTenant.Focus(); }
		}

		private void cbMainTenant_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; dpStart.Focus(); }
		}

		private void dpStart_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; dpEnd.Focus(); }
		}

		private void dpEnd_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtRent.Focus(); }
		}

		private void txtRent_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtDeposit.Focus(); }
		}

		private void txtDeposit_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; cbStatus.Focus(); }
		}

		private void cbStatus_KeyDown(object sender, KeyEventArgs e)
		{
			// Move keyboard focus for faster keyboard-driven form entry
			if (e.Key == Key.Enter) { e.Handled = true; txtNotes.Focus(); }
		}

		private void txtNotes_KeyDown(object sender, KeyEventArgs e)
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
