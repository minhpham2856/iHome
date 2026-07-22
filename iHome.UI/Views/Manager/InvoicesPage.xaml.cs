using iHome.BLL.DTOs.Manager;
using iHome.BLL.Services.Manager;
using iHome.DAL.Entities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace iHome.UI.Views.Manager
{
	// Trang hóa đơn: lọc, CRUD và xuất CSV
	public partial class InvoicesPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly InvoiceService _service = new();
		private readonly ContractService _contractService = new();
		private List<InvoiceDto> _invoices = new();
		private ICollectionView? _view;
		private bool _isLoading;

		// Khởi tạo theo user và tòa nhà đang chọn
		public InvoicesPage(User currentUser, int? buildingId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_buildingId = buildingId;
			Loaded += (_, _) => Load();
		}

		// Làm mới danh sách hóa đơn
		private void btnRefresh_Click(object sender, RoutedEventArgs e) => Load();

		// Tải hóa đơn và gắn bộ lọc
		private void Load()
		{
			if (_isLoading) return;
			try
			{
				_isLoading = true; btnRefresh.IsEnabled = false; SetState("Đang tải danh sách hóa đơn...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				var (ok, invoices, error) = ManagerUi.TryGet(() => _service.GetInvoices(managerId, _buildingId));
				if (!ok || invoices == null)
				{
					SetState(error ?? "Không thể tải danh sách hóa đơn.", true);
					return;
				}
				_invoices = invoices;
				_view = CollectionViewSource.GetDefaultView(_invoices); _view.Filter = FilterInvoice; dgInvoices.ItemsSource = _view;
				cbStatus.ItemsSource = new[] { All }.Concat(_invoices.Select(item => item.StatusDisplay).Distinct()); cbStatus.SelectedIndex = 0;
				UpdateSummary(); RefreshView();
			}
			finally { _isLoading = false; btnRefresh.IsEnabled = true; }
		}

		// Cập nhật KPI; còn phải thu chỉ cộng Balance chưa Paid
		private void UpdateSummary()
		{
			lbTotal.Text = _invoices.Count.ToString();
			lbUnpaid.Text = _invoices.Count(item => item.Status != "Paid").ToString();
			lbOverdue.Text = _invoices.Count(item => item.StatusDisplay == "Quá hạn").ToString();
			// KPI Còn phải thu: chỉ cộng Balance của hóa đơn chưa Paid
			lbOutstanding.Text = $"{_invoices.Where(item => item.Status != "Paid").Sum(item => Math.Max(0, item.Balance)):N0} đ";
		}

		// Điều kiện lọc theo từ khóa và trạng thái
		private bool FilterInvoice(object item)
		{
			if (item is not InvoiceDto invoice) return false;
			string keyword = txtSearch.Text.Trim();
			bool search = string.IsNullOrEmpty(keyword) || invoice.Id.ToString().Contains(keyword) || invoice.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || invoice.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) || invoice.MainTenantName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool status = cbStatus.SelectedItem is not string selected || selected == All || selected == invoice.StatusDisplay;
			return search && status;
		}

		// Áp lại bộ lọc khi tìm kiếm / combo đổi
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();

		// Làm mới view và đếm số dòng
		private void RefreshView() { _view?.Refresh(); int count = _view?.Cast<object>().Count() ?? 0; lbResultCount.Text = $"{count} hóa đơn"; SetState(count == 0 ? "Không có hóa đơn phù hợp." : string.Empty, count == 0); }

		// Tạo hóa đơn mới từ draft hợp đồng
		private void AddInvoice_Click(object sender, RoutedEventArgs e)
		{
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (ok, contracts, error) = ManagerUi.TryGet(() => _contractService.GetContractOptions(managerId, _buildingId));
			if (!ok || contracts == null)
			{
				ManagerUi.ShowError(error ?? "Không thể tải danh sách hợp đồng.");
				return;
			}
			var dialog = new InvoiceDialog(contracts, managerId, _service) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (ManagerUi.TryRun(() => { _service.CreateInvoice(managerId, dialog.Result); }))
				{
					Load();
				}
			}
		}

		// Sửa hóa đơn đang chọn
		private void EditInvoice_Click(object sender, RoutedEventArgs e)
		{
			if (dgInvoices.SelectedItem is not InvoiceDto selected) { ShowSelect(); return; }
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (formOk, form, formError) = ManagerUi.TryGet(() => _service.GetInvoice(managerId, selected.Id));
			var (contractsOk, contracts, contractsError) = ManagerUi.TryGet(() => _contractService.GetContractOptions(managerId, _buildingId));
			if (!formOk || !contractsOk || form == null || contracts == null)
			{
				ManagerUi.ShowError(formError ?? contractsError ?? "Không thể tải dữ liệu hóa đơn.");
				return;
			}
			// Hợp đồng có thể đã hết trong options — vẫn thêm để sửa được
			if (contracts.All(item => item.Id != form.ContractId))
			{
				contracts.Add(new ContractOptionDto { Id = form.ContractId, DisplayName = $"HĐ #{form.ContractId}" });
			}
			var dialog = new InvoiceDialog(contracts, managerId, _service, form) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (ManagerUi.TryRun(() => _service.UpdateInvoice(managerId, dialog.Result)))
				{
					Load();
				}
			}
		}

		// Xóa hóa đơn sau khi xác nhận
		private void DeleteInvoice_Click(object sender, RoutedEventArgs e)
		{
			if (dgInvoices.SelectedItem is not InvoiceDto selected) { ShowSelect(); return; }
			if (MessageBox.Show($"Xóa hóa đơn #{selected.Id}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			if (ManagerUi.TryRun(() => _service.DeleteInvoice(managerId, selected.Id)))
			{
				Load();
			}
		}

		// Xuất hóa đơn đang chọn ra CSV UTF-8
		private void ExportInvoice_Click(object sender, RoutedEventArgs e)
		{
			if (dgInvoices.SelectedItem is not InvoiceDto selected)
			{
				ShowSelect();
				return;
			}

			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (ok, invoice, error) = ManagerUi.TryGet(() => _service.GetInvoice(managerId, selected.Id));
			if (!ok || invoice == null)
			{
				ManagerUi.ShowError(error ?? "Không thể tải hóa đơn.");
				return;
			}
			var dialog = new SaveFileDialog
			{
				Title = "Xuất hóa đơn CSV",
				Filter = "CSV UTF-8 (*.csv)|*.csv",
				DefaultExt = ".csv",
				AddExtension = true,
				FileName = $"HoaDon_{selected.Id}_{selected.InvoiceDate:yyyy-MM}.csv"
			};
			if (dialog.ShowDialog(Window.GetWindow(this)) != true)
			{
				return;
			}

			try
			{
				string csv = BuildInvoiceCsv(selected, invoice);
				File.WriteAllText(dialog.FileName, csv, new UTF8Encoding(true));
				MessageBox.Show(
					$"Đã xuất hóa đơn #{selected.Id} ra file CSV.",
					"Xuất CSV thành công",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				ManagerUi.ShowError(ex.Message);
			}
		}

		// Ghép nội dung CSV từ tóm tắt + dòng chi tiết
		private static string BuildInvoiceCsv(
			InvoiceDto summary,
			InvoiceFormDto invoice)
		{
			var csv = new StringBuilder();
			csv.AppendLine(CsvRow("HÓA ĐƠN IHOME"));
			csv.AppendLine(CsvRow("Mã hóa đơn", summary.Id.ToString(CultureInfo.InvariantCulture)));
			csv.AppendLine(CsvRow("Mã hợp đồng", summary.ContractId.ToString(CultureInfo.InvariantCulture)));
			csv.AppendLine(CsvRow("Tòa nhà", summary.BuildingName));
			csv.AppendLine(CsvRow("Phòng", summary.RoomNumber));
			csv.AppendLine(CsvRow("Người thuê chính", summary.MainTenantName));
			csv.AppendLine(CsvRow("Ngày lập", summary.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
			csv.AppendLine(CsvRow("Hạn thanh toán", summary.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
			csv.AppendLine(CsvRow("Trạng thái", summary.StatusDisplay));
			csv.AppendLine();
			csv.AppendLine(CsvRow("STT", "Khoản thu", "Số lượng", "Đơn giá", "Thành tiền"));

			int index = 1;
			foreach (var item in invoice.Items)
			{
				csv.AppendLine(CsvRow(
					index.ToString(CultureInfo.InvariantCulture),
					item.Description,
					item.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
					item.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture),
					item.Amount.ToString("0.##", CultureInfo.InvariantCulture)));
				index++;
			}

			csv.AppendLine(CsvRow(
				string.Empty,
				"TỔNG CỘNG",
				string.Empty,
				string.Empty,
				invoice.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture)));
			return csv.ToString();
		}

		// Nối các ô thành một dòng CSV
		private static string CsvRow(params string?[] values) =>
			string.Join(",", values.Select(EscapeCsvCell));

		// Escape CSV; chặn formula injection khi ô bắt đầu =+-@
		private static string EscapeCsvCell(string? value)
		{
			string safeValue = value ?? string.Empty;
			if (safeValue.Length > 0 && "=+-@\t\r".Contains(safeValue[0]))
			{
				safeValue = $"'{safeValue}";
			}
			return $"\"{safeValue.Replace("\"", "\"\"")}\"";
		}

		// Hiện / ẩn thông báo trạng thái
		private void SetState(string message, bool visible) { lbState.Text = message; brdState.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }

		// Nhắc chọn hóa đơn trước khi thao tác
		private static void ShowSelect() => MessageBox.Show("Vui lòng chọn hóa đơn.", "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
