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

		// Nạp form tạo mới (draft) hoặc sửa hóa đơn có sẵn
		public InvoiceDialog(
			IEnumerable<ContractOptionDto> contracts,
			int managerId,
			InvoiceService service,
			InvoiceFormDto? invoice = null)
		{
			_managerId = managerId;
			_service = service;
			_invoiceId = invoice?.Id ?? 0;
			_isEdit = invoice != null;
			_suppressBillingChanges = true;
			InitializeComponent();

			cbContract.ItemsSource = contracts.ToList();
			cbStatus.ItemsSource = new[]
			{
				new StatusOption("Unpaid", "Chưa thanh toán"),
				new StatusOption("Paid", "Đã thanh toán")
			};

			if (invoice == null)
			{
				cbContract.SelectedIndex = cbContract.Items.Count > 0 ? 0 : -1;
				dpInvoiceDate.SelectedDate = DateTime.Today;
				dpDueDate.SelectedDate = DateTime.Today.AddDays(7);
				cbStatus.SelectedIndex = 0;
				_suppressBillingChanges = false;
				Loaded += (_, _) => LoadDraft();
				return;
			}

			lbTitle.Text = "Cập nhật hóa đơn";
			cbContract.SelectedItem = cbContract.Items.Cast<ContractOptionDto>()
				.FirstOrDefault(item => item.Id == invoice.ContractId);
			// Sửa: khóa hợp đồng và tháng lập (không đổi draft)
			cbContract.IsEnabled = false;
			dpInvoiceDate.SelectedDate = invoice.InvoiceDate;
			dpInvoiceDate.IsEnabled = false;
			dpDueDate.SelectedDate = invoice.DueDate.ToDateTime(TimeOnly.MinValue);
			cbStatus.SelectedItem = cbStatus.Items.Cast<StatusOption>()
				.First(item => item.Code == invoice.Status);
			_items = invoice.Items;
			dgItems.ItemsSource = _items;
			lbTotal.Text = $"{invoice.TotalAmount:N0} đ";
			_suppressBillingChanges = false;
		}

		// Đổi hợp đồng hoặc tháng lập → tính lại draft (chỉ khi tạo mới)
		private void BillingInputChanged(object sender, EventArgs e)
		{
			if (_suppressBillingChanges || _isEdit)
			{
				return;
			}
			LoadDraft();
		}

		// Gọi BLL lấy draft; bỏ kết quả cũ nếu version đã đổi
		private void LoadDraft()
		{
			if (cbContract.SelectedItem is not ContractOptionDto contract ||
				!dpInvoiceDate.SelectedDate.HasValue)
			{
				_items.Clear();
				dgItems.ItemsSource = null;
				lbTotal.Text = "0 đ";
				return;
			}

			int version = ++_draftVersion;
			int contractId = contract.Id;
			DateTime invoiceDate = dpInvoiceDate.SelectedDate.Value;
			SetState("Đang tính tiền phòng và dịch vụ...", true);
			btnSave.IsEnabled = false;
			var (ok, draft, error) = ManagerUi.TryGet(() => _service.GetInvoiceDraft(
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
				dgItems.ItemsSource = null;
				lbTotal.Text = "0 đ";
				SetState(error ?? "Không thể tính hóa đơn.", true);
				btnSave.IsEnabled = false;
				return;
			}

			_items = draft.Items;
			dgItems.ItemsSource = _items;
			dpDueDate.SelectedDate = draft.DueDate.ToDateTime(TimeOnly.MinValue);
			RecalculateTotal();
			SetState(string.Empty, false);
			btnSave.IsEnabled = _items.Count > 0;
		}

		// Chỉ số cũ chỉ sửa được khi CanEditPreviousReading (chưa có trong DB)
		private void PreviousReading_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_isRecalculating || sender is not TextBox textBox ||
				textBox.DataContext is not InvoiceLineDto item ||
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

		// Nhập chỉ số mới → tính lại tiêu thụ và thành tiền dòng Metered
		private void CurrentReading_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_isRecalculating || sender is not TextBox textBox ||
				textBox.DataContext is not InvoiceLineDto item ||
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
		private static void RecalculateMeteredLine(InvoiceLineDto item)
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

		// Cộng tổng các dòng; _isRecalculating tránh vòng TextChanged
		private void RecalculateTotal()
		{
			_isRecalculating = true;
			try
			{
				lbTotal.Text = $"{_items.Sum(item => item.Amount):N0} đ";
			}
			finally
			{
				_isRecalculating = false;
			}
		}

		// Kiểm tra chỉ số Metered rồi trả Result
		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (cbContract.SelectedItem is not ContractOptionDto contract ||
				cbStatus.SelectedItem is not StatusOption status ||
				!dpInvoiceDate.SelectedDate.HasValue ||
				!dpDueDate.SelectedDate.HasValue ||
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

			// Chỉ số mới phải >= chỉ số cũ
			var invalidReading = _items.FirstOrDefault(item =>
				item.RequiresReading &&
				(!item.CurrentReading.HasValue || item.CurrentReading.Value < (item.PreviousReading ?? 0)));
			if (invalidReading != null)
			{
				ManagerUi.ShowValidation($"Chỉ số mới của {invalidReading.Description} phải lớn hơn hoặc bằng chỉ số cũ.");
				return;
			}

			var form = new InvoiceFormDto
			{
				Id = _invoiceId,
				ContractId = contract.Id,
				InvoiceDate = dpInvoiceDate.SelectedDate.Value,
				DueDate = DateOnly.FromDateTime(dpDueDate.SelectedDate.Value),
				TotalAmount = _items.Sum(item => item.Amount),
				Status = status.Code,
				Items = _items
			};
			string? error = FormValidation.GetInvoiceError(form, !_isEdit);
			if (error != null)
			{
				ManagerUi.ShowValidation(error);
				return;
			}

			Result = form;
			DialogResult = true;
		}

		// Hiện / ẩn thông báo trạng thái draft
		private void SetState(string message, bool visible)
		{
			lbState.Text = message;
			brdState.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		}

		// Parse số thập phân theo culture hiện tại hoặc Invariant
		private static bool TryParseDecimal(string value, out decimal result) =>
			decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) ||
			decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

		// Đóng dialog không lưu
		private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

		private sealed record StatusOption(string Code, string Name);
	}
}
