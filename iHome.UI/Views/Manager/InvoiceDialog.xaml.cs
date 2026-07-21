using iHome.BLL.DTOs;
using iHome.BLL.Services.Manager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
		private readonly ManagerInvoiceService _service;
		private readonly bool _isEdit;
		private List<ManagerInvoiceLineDto> _items = new();
		private bool _suppressBillingChanges;
		private bool _isRecalculating;
		// Chống race khi user đổi hợp đồng/tháng liên tục trong lúc đang load draft
		private int _draftVersion;

		public ManagerInvoiceFormDto? Result { get; private set; }

		public InvoiceDialog(
			IEnumerable<ManagerContractOptionDto> contracts,
			int managerId,
			ManagerInvoiceService service,
			ManagerInvoiceFormDto? invoice = null)
		{
			_managerId = managerId;
			_service = service;
			_invoiceId = invoice?.Id ?? 0;
			_isEdit = invoice != null;
			_suppressBillingChanges = true;
			InitializeComponent();

			CbContract.ItemsSource = contracts.ToList();
			CbStatus.ItemsSource = new[]
			{
				new StatusOption("Unpaid", "Chưa thanh toán"),
				new StatusOption("Paid", "Đã thanh toán")
			};

			if (invoice == null)
			{
				CbContract.SelectedIndex = CbContract.Items.Count > 0 ? 0 : -1;
				DtpInvoiceDate.SelectedDate = DateTime.Today;
				DtpDueDate.SelectedDate = DateTime.Today.AddDays(7);
				CbStatus.SelectedIndex = 0;
				_suppressBillingChanges = false;
				Loaded += async (_, _) => await LoadDraftAsync();
				return;
			}

			TxtTitle.Text = "Cập nhật hóa đơn";
			CbContract.SelectedItem = CbContract.Items.Cast<ManagerContractOptionDto>()
				.FirstOrDefault(item => item.Id == invoice.ContractId);
			CbContract.IsEnabled = false;
			DtpInvoiceDate.SelectedDate = invoice.InvoiceDate;
			DtpInvoiceDate.IsEnabled = false;
			DtpDueDate.SelectedDate = invoice.DueDate.ToDateTime(TimeOnly.MinValue);
			CbStatus.SelectedItem = CbStatus.Items.Cast<StatusOption>()
				.First(item => item.Code == invoice.Status);
			_items = invoice.Items;
			ItemsGrid.ItemsSource = _items;
			TxtTotal.Text = $"{invoice.TotalAmount:N0} đ";
			_suppressBillingChanges = false;
		}

		// Đổi hợp đồng hoặc tháng lập → tính lại draft (chỉ khi tạo mới)
		private async void BillingInputChanged(object sender, EventArgs e)
		{
			if (_suppressBillingChanges || _isEdit)
			{
				return;
			}
			await LoadDraftAsync();
		}

		private async Task LoadDraftAsync()
		{
			if (CbContract.SelectedItem is not ManagerContractOptionDto contract ||
				!DtpInvoiceDate.SelectedDate.HasValue)
			{
				_items.Clear();
				ItemsGrid.ItemsSource = null;
				TxtTotal.Text = "0 đ";
				return;
			}

			int version = ++_draftVersion;
			int contractId = contract.Id;
			DateTime invoiceDate = DtpInvoiceDate.SelectedDate.Value;
			SetState("Đang tính tiền phòng và dịch vụ...", true);
			BtnSave.IsEnabled = false;
			var (ok, draft, error) = await ManagerUi.TryGetAsync(() => _service.GetInvoiceDraft(
				_managerId,
				contractId,
				invoiceDate));
			// Bỏ kết quả cũ nếu user đã đổi lựa chọn trong lúc chờ
			if (version != _draftVersion)
			{
				return;
			}

			if (!ok || draft == null)
			{
				_items.Clear();
				ItemsGrid.ItemsSource = null;
				TxtTotal.Text = "0 đ";
				SetState(error ?? "Không thể tính hóa đơn.", true);
				BtnSave.IsEnabled = false;
				return;
			}

			_items = draft.Items;
			ItemsGrid.ItemsSource = _items;
			DtpDueDate.SelectedDate = draft.DueDate.ToDateTime(TimeOnly.MinValue);
			RecalculateTotal();
			SetState(string.Empty, false);
			BtnSave.IsEnabled = _items.Count > 0;
		}

		// Chỉ số cũ chỉ sửa được khi CanEditPreviousReading (chưa có trong DB)
		private void PreviousReading_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_isRecalculating || sender is not TextBox textBox ||
				textBox.DataContext is not ManagerInvoiceLineDto item ||
				!item.RequiresReading ||
				!item.CanEditPreviousReading)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(textBox.Text))
			{
				item.PreviousReading = null;
			}
			else if (TryParseDecimal(textBox.Text, out decimal previousReading))
			{
				item.PreviousReading = previousReading;
			}
			else
			{
				item.PreviousReading = null;
			}
			RecalculateMeteredLine(item);
			RecalculateTotal();
		}

		private void CurrentReading_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_isRecalculating || sender is not TextBox textBox ||
				textBox.DataContext is not ManagerInvoiceLineDto item ||
				!item.RequiresReading)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(textBox.Text))
			{
				item.CurrentReading = null;
			}
			else if (TryParseDecimal(textBox.Text, out decimal currentReading))
			{
				item.CurrentReading = currentReading;
			}
			else
			{
				item.CurrentReading = null;
			}
			RecalculateMeteredLine(item);
			RecalculateTotal();
		}

		// Sử dụng = chỉ số mới - chỉ số cũ; thành tiền = sử dụng * đơn giá
		private static void RecalculateMeteredLine(ManagerInvoiceLineDto item)
		{
			if (!item.CurrentReading.HasValue)
			{
				item.Quantity = 0;
				item.Amount = 0;
				return;
			}

			decimal previous = item.PreviousReading ?? 0m;
			item.Quantity = Math.Max(0, item.CurrentReading.Value - previous);
			item.Amount = item.Quantity * item.UnitPrice;
		}

		private void RecalculateTotal()
		{
			_isRecalculating = true;
			try
			{
				TxtTotal.Text = $"{_items.Sum(item => item.Amount):N0} đ";
			}
			finally
			{
				_isRecalculating = false;
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (CbContract.SelectedItem is not ManagerContractOptionDto contract ||
				CbStatus.SelectedItem is not StatusOption status ||
				!DtpInvoiceDate.SelectedDate.HasValue ||
				!DtpDueDate.SelectedDate.HasValue ||
				_items.Count == 0)
			{
				ManagerUi.ShowValidation("Vui lòng nhập đầy đủ thông tin hóa đơn.");
				return;
			}

			// Phòng/khách mới: bắt buộc nhập chỉ số cũ trước khi lưu
			var missingPrevious = _items.FirstOrDefault(item =>
				item.RequiresReading &&
				item.CanEditPreviousReading &&
				!item.PreviousReading.HasValue);
			if (missingPrevious != null)
			{
				ManagerUi.ShowValidation($"Vui lòng nhập chỉ số cũ cho {missingPrevious.Description}.");
				return;
			}

			var invalidReading = _items.FirstOrDefault(item =>
				item.RequiresReading &&
				(!item.CurrentReading.HasValue || item.CurrentReading.Value < (item.PreviousReading ?? 0)));
			if (invalidReading != null)
			{
				ManagerUi.ShowValidation($"Chỉ số mới của {invalidReading.Description} phải lớn hơn hoặc bằng chỉ số cũ.");
				return;
			}

			var form = new ManagerInvoiceFormDto
			{
				Id = _invoiceId,
				ContractId = contract.Id,
				InvoiceDate = DtpInvoiceDate.SelectedDate.Value,
				DueDate = DateOnly.FromDateTime(DtpDueDate.SelectedDate.Value),
				TotalAmount = _items.Sum(item => item.Amount),
				Status = status.Code,
				Items = _items
			};
			string? error = ManagerValidation.GetInvoiceError(form, !_isEdit);
			if (error != null)
			{
				ManagerUi.ShowValidation(error);
				return;
			}

			Result = form;
			DialogResult = true;
		}

		private void SetState(string message, bool visible)
		{
			StateText.Text = message;
			StatePanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		}

		private static bool TryParseDecimal(string value, out decimal result) =>
			decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) ||
			decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private sealed record StatusOption(string Code, string Name);
	}
}
