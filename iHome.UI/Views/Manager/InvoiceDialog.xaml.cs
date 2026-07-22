using iHome.BLL.DTOs.Manager;
using iHome.BLL.Services.Manager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace iHome.UI.Views.Manager
{
	// Dialog lập/sửa hóa đơn: draft tự tính tiền phòng + dịch vụ;
	// dịch vụ Metered cho nhập chỉ số; chỉ số cũ editable nếu DB chưa có lịch sử
	public partial class InvoiceDialog : Window
	{
		private readonly int _managerId;
		private readonly int _invoiceId;
		private readonly InvoiceService _service;
		private readonly bool _isEdit;
		private List<InvoiceLineDto> _items = new();
		private bool _suppressBillingChanges;
		private bool _isRecalculating;
		// Chống race khi user đổi hợp đồng/tháng liên tục trong lúc đang load draft
		private int _draftVersion;

		public InvoiceFormDto? Result { get; private set; }

		public InvoiceDialog(
			IEnumerable<ContractOptionDto> contracts,
			int managerId,
			InvoiceService service,
			InvoiceFormDto? invoice = null)
		{
			// Assign local/page state inside InvoiceDialog without altering business rules
			_managerId = managerId;
			// Assign local/page state inside InvoiceDialog without altering business rules
			_service = service;
			// Assign local/page state inside InvoiceDialog without altering business rules
			_invoiceId = invoice?.Id ?? 0;
			// Assign local/page state inside InvoiceDialog without altering business rules
			_isEdit = invoice != null;
			// Flip internal flag to suppress duplicate events or mark in-flight operation
			_suppressBillingChanges = true;
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();

			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbContract.ItemsSource = contracts.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			cbStatus.ItemsSource = new[]
			{
				// Execute UI step inside InvoiceDialog
				new StatusOption("Unpaid", "Chưa thanh toán"),
				// Execute UI step inside InvoiceDialog
				new StatusOption("Paid", "Đã thanh toán")
			};

			// Guard clause: only continue when UI selection, role, or input is valid
			if (invoice == null)
			{
				// Pick default combo index (usually first/all option) after reload
				cbContract.SelectedIndex = cbContract.Items.Count > 0 ? 0 : -1;
				// Convert between DatePicker DateTime and DateOnly DTO fields
				dpInvoiceDate.SelectedDate = DateTime.Today;
				// Convert between DatePicker DateTime and DateOnly DTO fields
				dpDueDate.SelectedDate = DateTime.Today.AddDays(7);
				// Pick default combo index (usually first/all option) after reload
				cbStatus.SelectedIndex = 0;
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressBillingChanges = false;
				// Subscribe page Loaded event to defer BLL calls until controls exist
				Loaded += (_, _) => LoadDraft();
				// Exit method early or return value/tuple to caller
				return;
			}

			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTitle.Text = "Cập nhật hóa đơn";
			// Restore or set combo selection to match entity id or filter
			cbContract.SelectedItem = cbContract.Items.Cast<ContractOptionDto>()
				// Assign local/page state inside InvoiceDialog without altering business rules
				.FirstOrDefault(item => item.Id == invoice.ContractId);
			// Enable/disable control during loading or when prerequisites missing
			cbContract.IsEnabled = false;
			// Assign local/page state inside InvoiceDialog without altering business rules
			dpInvoiceDate.SelectedDate = invoice.InvoiceDate;
			// Enable/disable control during loading or when prerequisites missing
			dpInvoiceDate.IsEnabled = false;
			// Convert between DatePicker DateTime and DateOnly DTO fields
			dpDueDate.SelectedDate = invoice.DueDate.ToDateTime(TimeOnly.MinValue);
			// Restore or set combo selection to match entity id or filter
			cbStatus.SelectedItem = cbStatus.Items.Cast<StatusOption>()
				// Assign local/page state inside InvoiceDialog without altering business rules
				.First(item => item.Code == invoice.Status);
			// Assign local/page state inside InvoiceDialog without altering business rules
			_items = invoice.Items;
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgItems.ItemsSource = _items;
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotal.Text = $"{invoice.TotalAmount:N0} đ";
			// Flip internal flag to suppress duplicate events or mark in-flight operation
			_suppressBillingChanges = false;
		}

		// Đổi hợp đồng hoặc tháng lập → tính lại draft (chỉ khi tạo mới)
		private void BillingInputChanged(object sender, EventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_suppressBillingChanges || _isEdit)
			{
				// Exit method early or return value/tuple to caller
				return;
			}
			// Call helper LoadDraft to refresh UI state from BLL data
			LoadDraft();
		}

		private void LoadDraft()
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbContract.SelectedItem is not ContractOptionDto contract ||
				// Execute UI step inside LoadDraft
				!dpInvoiceDate.SelectedDate.HasValue)
			{
				// Execute UI step inside LoadDraft
				_items.Clear();
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgItems.ItemsSource = null;
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbTotal.Text = "0 đ";
				// Exit method early or return value/tuple to caller
				return;
			}

			// Flip internal flag to suppress duplicate events or mark in-flight operation
			int version = ++_draftVersion;
			// Assign local/page state inside LoadDraft without altering business rules
			int contractId = contract.Id;
			// Convert between DatePicker DateTime and DateOnly DTO fields
			DateTime invoiceDate = dpInvoiceDate.SelectedDate.Value;
			// Call helper SetState to refresh UI state from BLL data
			SetState("Đang tính tiền phòng và dịch vụ...", true);
			// Enable/disable control during loading or when prerequisites missing
			btnSave.IsEnabled = false;
			// Call page BLL service GetInvoiceDraft to load or mutate scoped data
			var (ok, draft, error) = ManagerUi.TryGet(() => _service.GetInvoiceDraft(
				// Execute UI step inside LoadDraft
				_managerId,
				// Execute UI step inside LoadDraft
				contractId,
				// Execute UI step inside LoadDraft
				invoiceDate));
			// Bỏ kết quả cũ nếu user đã đổi lựa chọn trong lúc chờ
			if (version != _draftVersion)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// Guard clause: only continue when UI selection, role, or input is valid
			if (!ok || draft == null)
			{
				// Execute UI step inside LoadDraft
				_items.Clear();
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgItems.ItemsSource = null;
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbTotal.Text = "0 đ";
				// Call helper SetState to refresh UI state from BLL data
				SetState(error ?? "Không thể tính hóa đơn.", true);
				// Enable/disable control during loading or when prerequisites missing
				btnSave.IsEnabled = false;
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside LoadDraft without altering business rules
			_items = draft.Items;
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			dgItems.ItemsSource = _items;
			// Convert between DatePicker DateTime and DateOnly DTO fields
			dpDueDate.SelectedDate = draft.DueDate.ToDateTime(TimeOnly.MinValue);
			// Recompute line amounts and invoice total after meter reading edit
			RecalculateTotal();
			// Call helper SetState to refresh UI state from BLL data
			SetState(string.Empty, false);
			// Enable/disable control during loading or when prerequisites missing
			btnSave.IsEnabled = _items.Count > 0;
		}

		// Chỉ số cũ chỉ sửa được khi CanEditPreviousReading (chưa có trong DB)
		private void PreviousReading_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isRecalculating || sender is not TextBox textBox ||
				// Work with BLL DTO/form object returned from service or built from controls
				textBox.DataContext is not InvoiceLineDto item ||
				// Execute UI step inside PreviousReading_TextChanged
				!item.RequiresReading ||
				// Execute UI step inside PreviousReading_TextChanged
				!item.CanEditPreviousReading)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// Guard clause: only continue when UI selection, role, or input is valid
			if (string.IsNullOrWhiteSpace(textBox.Text))
			{
				// Assign local/page state inside PreviousReading_TextChanged without altering business rules
				item.PreviousReading = null;
			}
			// Alternate branch when previous condition was not satisfied
			else if (TryParseDecimal(textBox.Text, out decimal previousReading))
			{
				// Assign local/page state inside PreviousReading_TextChanged without altering business rules
				item.PreviousReading = previousReading;
			}
			// Alternate branch when previous condition was not satisfied
			else
			{
				// Assign local/page state inside PreviousReading_TextChanged without altering business rules
				item.PreviousReading = null;
			}
			// Recompute line amounts and invoice total after meter reading edit
			RecalculateMeteredLine(item);
			// Recompute line amounts and invoice total after meter reading edit
			RecalculateTotal();
		}

		private void CurrentReading_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isRecalculating || sender is not TextBox textBox ||
				// Work with BLL DTO/form object returned from service or built from controls
				textBox.DataContext is not InvoiceLineDto item ||
				// Execute UI step inside CurrentReading_TextChanged
				!item.RequiresReading)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// Guard clause: only continue when UI selection, role, or input is valid
			if (string.IsNullOrWhiteSpace(textBox.Text))
			{
				// Assign local/page state inside CurrentReading_TextChanged without altering business rules
				item.CurrentReading = null;
			}
			// Alternate branch when previous condition was not satisfied
			else if (TryParseDecimal(textBox.Text, out decimal currentReading))
			{
				// Assign local/page state inside CurrentReading_TextChanged without altering business rules
				item.CurrentReading = currentReading;
			}
			// Alternate branch when previous condition was not satisfied
			else
			{
				// Assign local/page state inside CurrentReading_TextChanged without altering business rules
				item.CurrentReading = null;
			}
			// Recompute line amounts and invoice total after meter reading edit
			RecalculateMeteredLine(item);
			// Recompute line amounts and invoice total after meter reading edit
			RecalculateTotal();
		}

		// Sử dụng = chỉ số mới - chỉ số cũ; thành tiền = sử dụng * đơn giá
		private static void RecalculateMeteredLine(InvoiceLineDto item)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!item.CurrentReading.HasValue)
			{
				// Assign local/page state inside RecalculateMeteredLine without altering business rules
				item.Quantity = 0;
				// Assign local/page state inside RecalculateMeteredLine without altering business rules
				item.Amount = 0;
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside RecalculateMeteredLine without altering business rules
			decimal previous = item.PreviousReading ?? 0m;
			// Assign local/page state inside RecalculateMeteredLine without altering business rules
			item.Quantity = Math.Max(0, item.CurrentReading.Value - previous);
			// Assign local/page state inside RecalculateMeteredLine without altering business rules
			item.Amount = item.Quantity * item.UnitPrice;
		}

		private void RecalculateTotal()
		{
			// Flip internal flag to suppress duplicate events or mark in-flight operation
			_isRecalculating = true;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbTotal.Text = $"{_items.Sum(item => item.Amount):N0} đ";
			}
			// Always reset loading flags and re-enable refresh regardless of success
			finally
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isRecalculating = false;
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (cbContract.SelectedItem is not ContractOptionDto contract ||
				// Change combo selection to drive filter cascade or dialog default
				cbStatus.SelectedItem is not StatusOption status ||
				// Execute UI step inside Save_Click
				!dpInvoiceDate.SelectedDate.HasValue ||
				// Execute UI step inside Save_Click
				!dpDueDate.SelectedDate.HasValue ||
				// Assign local/page state inside Save_Click without altering business rules
				_items.Count == 0)
			{
				// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowValidation("Vui lòng nhập đầy đủ thông tin hóa đơn.");
				// Exit method early or return value/tuple to caller
				return;
			}

			// Phòng/khách mới: bắt buộc nhập chỉ số cũ trước khi lưu
			var missingPrevious = _items.FirstOrDefault(item =>
				// Execute UI step inside Save_Click
				item.RequiresReading &&
				// Execute UI step inside Save_Click
				item.CanEditPreviousReading &&
				// Execute UI step inside Save_Click
				!item.PreviousReading.HasValue);
			// Guard clause: only continue when UI selection, role, or input is valid
			if (missingPrevious != null)
			{
				// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowValidation($"Vui lòng nhập chỉ số cũ cho {missingPrevious.Description}.");
				// Exit method early or return value/tuple to caller
				return;
			}

			// Assign local/page state inside Save_Click without altering business rules
			var invalidReading = _items.FirstOrDefault(item =>
				// Execute UI step inside Save_Click
				item.RequiresReading &&
				// Execute UI step inside Save_Click
				(!item.CurrentReading.HasValue || item.CurrentReading.Value < (item.PreviousReading ?? 0)));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (invalidReading != null)
			{
				// ManagerUi.ShowValidation shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowValidation($"Chỉ số mới của {invalidReading.Description} phải lớn hơn hoặc bằng chỉ số cũ.");
				// Exit method early or return value/tuple to caller
				return;
			}

			// Work with BLL DTO/form object returned from service or built from controls
			var form = new InvoiceFormDto
			{
				// Assign local/page state inside Save_Click without altering business rules
				Id = _invoiceId,
				// Assign local/page state inside Save_Click without altering business rules
				ContractId = contract.Id,
				// Assign local/page state inside Save_Click without altering business rules
				InvoiceDate = dpInvoiceDate.SelectedDate.Value,
				// Convert between DatePicker DateTime and DateOnly DTO fields
				DueDate = DateOnly.FromDateTime(dpDueDate.SelectedDate.Value),
				// Aggregate list into KPI number shown on summary labels
				TotalAmount = _items.Sum(item => item.Amount),
				// Assign local/page state inside Save_Click without altering business rules
				Status = status.Code,
				// Assign local/page state inside Save_Click without altering business rules
				Items = _items
			};
			// FormValidation.GetInvoiceError validates dialog DTO before accepting save
			string? error = FormValidation.GetInvoiceError(form, !_isEdit);
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

		private void SetState(string message, bool visible)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbState.Text = message;
			// Show or hide panel/border for empty state or role-specific UI
			brdState.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		}

		private static bool TryParseDecimal(string value, out decimal result) =>
			decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) ||
			decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

		// Cancel dialog without persisting changes
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private sealed record StatusOption(string Code, string Name);
	}
}
