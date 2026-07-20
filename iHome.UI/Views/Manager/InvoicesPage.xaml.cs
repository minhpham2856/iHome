using iHome.BLL.DTOs;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace iHome.UI.Views.Manager
{
	public partial class InvoicesPage : Page
	{
		private const string All = "Tất cả";
		private readonly User _currentUser;
		private readonly int? _propertyId;
		private readonly ManagerInvoiceService _service = new();
		private readonly ManagerContractService _contractService = new();
		private List<ManagerInvoiceDto> _invoices = new();
		private ICollectionView? _view;
		private bool _isLoading;

		public InvoicesPage(User currentUser, int? propertyId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_propertyId = propertyId;
			Loaded += async (_, _) => await LoadAsync();
		}

		private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

		private async Task LoadAsync()
		{
			if (_isLoading) return;
			try
			{
				_isLoading = true; BtnRefresh.IsEnabled = false; SetState("Đang tải danh sách hóa đơn...", true);
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				var (ok, invoices, error) = await ManagerUi.TryGetAsync(() => _service.GetInvoices(managerId, _propertyId));
				if (!ok || invoices == null)
				{
					SetState(error ?? "Không thể tải danh sách hóa đơn.", true);
					return;
				}
				_invoices = invoices;
				_view = CollectionViewSource.GetDefaultView(_invoices); _view.Filter = FilterInvoice; InvoicesGrid.ItemsSource = _view;
				CboStatus.ItemsSource = new[] { All }.Concat(_invoices.Select(item => item.StatusDisplay).Distinct()); CboStatus.SelectedIndex = 0;
				UpdateSummary(); RefreshView();
			}
			finally { _isLoading = false; BtnRefresh.IsEnabled = true; }
		}

		private void UpdateSummary()
		{
			TxtTotal.Text = _invoices.Count.ToString();
			TxtUnpaid.Text = _invoices.Count(item => item.Status != "Paid").ToString();
			TxtOverdue.Text = _invoices.Count(item => item.StatusDisplay == "Quá hạn").ToString();
			// Chỉ cộng số còn lại của hóa đơn chưa thanh toán
			TxtOutstanding.Text = $"{_invoices.Where(item => item.Status != "Paid").Sum(item => Math.Max(0, item.Balance)):N0} đ";
		}

		private bool FilterInvoice(object item)
		{
			if (item is not ManagerInvoiceDto invoice) return false;
			string keyword = TxtSearch.Text.Trim();
			bool search = string.IsNullOrEmpty(keyword) || invoice.Id.ToString().Contains(keyword) || invoice.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || invoice.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) || invoice.MainTenantName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			bool status = CboStatus.SelectedItem is not string selected || selected == All || selected == invoice.StatusDisplay;
			return search && status;
		}

		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();
		private void RefreshView() { _view?.Refresh(); int count = _view?.Cast<object>().Count() ?? 0; TxtResultCount.Text = $"{count} hóa đơn"; SetState(count == 0 ? "Không có hóa đơn phù hợp." : string.Empty, count == 0); }

		private async void AddInvoice_Click(object sender, RoutedEventArgs e)
		{
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (ok, contracts, error) = await ManagerUi.TryGetAsync(() => _contractService.GetContractOptions(managerId, _propertyId));
			if (!ok || contracts == null)
			{
				ManagerUi.ShowError(error ?? "Không thể tải danh sách hợp đồng.");
				return;
			}
			var dialog = new InvoiceDialog(contracts, managerId, _service) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (await ManagerUi.TryRunAsync(() => { _service.CreateInvoice(managerId, dialog.Result); }))
				{
					await LoadAsync();
				}
			}
		}

		private async void EditInvoice_Click(object sender, RoutedEventArgs e)
		{
			if (InvoicesGrid.SelectedItem is not ManagerInvoiceDto selected) { ShowSelect(); return; }
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (formOk, form, formError) = await ManagerUi.TryGetAsync(() => _service.GetInvoice(managerId, selected.Id));
			var (contractsOk, contracts, contractsError) = await ManagerUi.TryGetAsync(() => _contractService.GetContractOptions(managerId, _propertyId));
			if (!formOk || !contractsOk || form == null || contracts == null)
			{
				ManagerUi.ShowError(formError ?? contractsError ?? "Không thể tải dữ liệu hóa đơn.");
				return;
			}
			if (contracts.All(item => item.Id != form.ContractId))
			{
				contracts.Add(new ManagerContractOptionDto { Id = form.ContractId, DisplayName = $"HĐ #{form.ContractId}" });
			}
			var dialog = new InvoiceDialog(contracts, managerId, _service, form) { Owner = Window.GetWindow(this) };
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				if (await ManagerUi.TryRunAsync(() => _service.UpdateInvoice(managerId, dialog.Result)))
				{
					await LoadAsync();
				}
			}
		}

		private async void DeleteInvoice_Click(object sender, RoutedEventArgs e)
		{
			if (InvoicesGrid.SelectedItem is not ManagerInvoiceDto selected) { ShowSelect(); return; }
			if (MessageBox.Show($"Xóa hóa đơn #{selected.Id}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			if (await ManagerUi.TryRunAsync(() => _service.DeleteInvoice(managerId, selected.Id)))
			{
				await LoadAsync();
			}
		}

		private async void ExportInvoice_Click(object sender, RoutedEventArgs e)
		{
			if (InvoicesGrid.SelectedItem is not ManagerInvoiceDto selected)
			{
				ShowSelect();
				return;
			}

			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			var (ok, invoice, error) = await ManagerUi.TryGetAsync(() => _service.GetInvoice(managerId, selected.Id));
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
				await File.WriteAllTextAsync(dialog.FileName, csv, new UTF8Encoding(true));
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

		private static string BuildInvoiceCsv(
			ManagerInvoiceDto summary,
			ManagerInvoiceFormDto invoice)
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

		private static string CsvRow(params string?[] values) =>
			string.Join(",", values.Select(EscapeCsvCell));

		private static string EscapeCsvCell(string? value)
		{
			string safeValue = value ?? string.Empty;
			if (safeValue.Length > 0 && "=+-@\t\r".Contains(safeValue[0]))
			{
				safeValue = $"'{safeValue}";
			}
			return $"\"{safeValue.Replace("\"", "\"\"")}\"";
		}

		private void SetState(string message, bool visible) { StateText.Text = message; StatePanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }
		private static void ShowSelect() => MessageBox.Show("Vui lòng chọn hóa đơn.", "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
