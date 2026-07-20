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
	public partial class InvoiceDialog : Window
	{
		private readonly int _managerId;
		private readonly int _invoiceId;
		private readonly ManagerInvoiceService _service;
		private readonly bool _isEdit;
		private List<ManagerInvoiceLineDto> _items = new();
		private bool _suppressBillingChanges;
		private bool _isRecalculating;
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

			CboContract.ItemsSource = contracts.ToList();
			CboStatus.ItemsSource = new[]
			{
				new StatusOption("Unpaid", "Chưa thanh toán"),
				new StatusOption("Paid", "Đã thanh toán")
			};

			if (invoice == null)
			{
				CboContract.SelectedIndex = CboContract.Items.Count > 0 ? 0 : -1;
				DtpInvoiceDate.SelectedDate = DateTime.Today;
				DtpDueDate.SelectedDate = DateTime.Today.AddDays(7);
				CboStatus.SelectedIndex = 0;
				_suppressBillingChanges = false;
				Loaded += async (_, _) => await LoadDraftAsync();
				return;
			}

			TxtTitle.Text = "Cập nhật hóa đơn";
			CboContract.SelectedItem = CboContract.Items.Cast<ManagerContractOptionDto>()
				.FirstOrDefault(item => item.Id == invoice.ContractId);
			CboContract.IsEnabled = false;
			DtpInvoiceDate.SelectedDate = invoice.InvoiceDate;
			DtpInvoiceDate.IsEnabled = false;
			DtpDueDate.SelectedDate = invoice.DueDate.ToDateTime(TimeOnly.MinValue);
			CboStatus.SelectedItem = CboStatus.Items.Cast<StatusOption>()
				.First(item => item.Code == invoice.Status);
			_items = invoice.Items;
			ItemsGrid.ItemsSource = _items;
			TxtTotal.Text = $"{invoice.TotalAmount:N0} đ";
			_suppressBillingChanges = false;
		}

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
			if (CboContract.SelectedItem is not ManagerContractOptionDto contract ||
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
				item.Quantity = 0;
				item.Amount = 0;
			}
			else if (TryParseDecimal(textBox.Text, out decimal currentReading))
			{
				item.CurrentReading = currentReading;
				item.Quantity = Math.Max(0, currentReading - (item.PreviousReading ?? 0));
				item.Amount = item.Quantity * item.UnitPrice;
			}
			else
			{
				item.CurrentReading = null;
				item.Quantity = 0;
				item.Amount = 0;
			}
			RecalculateTotal();
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
			if (CboContract.SelectedItem is not ManagerContractOptionDto contract ||
				CboStatus.SelectedItem is not StatusOption status ||
				!DtpInvoiceDate.SelectedDate.HasValue ||
				!DtpDueDate.SelectedDate.HasValue ||
				_items.Count == 0)
			{
				ManagerUi.ShowValidation("Vui lòng nhập đầy đủ thông tin hóa đơn.");
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
