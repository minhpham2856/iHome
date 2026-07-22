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

		public InvoicesPage(User currentUser, int? buildingId)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside InvoicesPage without altering business rules
			_currentUser = currentUser;
			// Assign local/page state inside InvoicesPage without altering business rules
			_buildingId = buildingId;
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += (_, _) => Load();
		}

		// Assign local/page state inside btnRefresh_Click without altering business rules
		private void btnRefresh_Click(object sender, RoutedEventArgs e) => Load();

		private void Load()
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isLoading) return;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Enable/disable control during loading or when prerequisites missing
				_isLoading = true; btnRefresh.IsEnabled = false; SetState("Đang tải danh sách hóa đơn...", true);
				// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				// Call page BLL service GetInvoices to load or mutate scoped data
				var (ok, invoices, error) = ManagerUi.TryGet(() => _service.GetInvoices(managerId, _buildingId));
				// Guard clause: only continue when UI selection, role, or input is valid
				if (!ok || invoices == null)
				{
					// Call helper SetState to refresh UI state from BLL data
					SetState(error ?? "Không thể tải danh sách hóa đơn.", true);
					// Exit method early or return value/tuple to caller
					return;
				}
				// Assign local/page state inside Load without altering business rules
				_invoices = invoices;
				// Wrap list in ICollectionView to enable client-side Filter
				_view = CollectionViewSource.GetDefaultView(_invoices); _view.Filter = FilterInvoice; dgInvoices.ItemsSource = _view;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				cbStatus.ItemsSource = new[] { All }.Concat(_invoices.Select(item => item.StatusDisplay).Distinct()); cbStatus.SelectedIndex = 0;
				// Call helper UpdateSummary to refresh UI state from BLL data
				UpdateSummary(); RefreshView();
			}
			// Enable/disable control during loading or when prerequisites missing
			finally { _isLoading = false; btnRefresh.IsEnabled = true; }
		}

		private void UpdateSummary()
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotal.Text = _invoices.Count.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbUnpaid.Text = _invoices.Count(item => item.Status != "Paid").ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOverdue.Text = _invoices.Count(item => item.StatusDisplay == "Quá hạn").ToString();
			// KPI Còn phải thu: chỉ cộng Balance của hóa đơn chưa Paid (không gồm đã thanh toán)
			lbOutstanding.Text = $"{_invoices.Where(item => item.Status != "Paid").Sum(item => Math.Max(0, item.Balance)):N0} đ";
		}

		private bool FilterInvoice(object item)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (item is not InvoiceDto invoice) return false;
			// Update TextBlock/TextBox caption or read user-entered text from control
			string keyword = txtSearch.Text.Trim();
			// Assign local/page state inside FilterInvoice without altering business rules
			bool search = string.IsNullOrEmpty(keyword) || invoice.Id.ToString().Contains(keyword) || invoice.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase) || invoice.RoomNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) || invoice.MainTenantName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
			// Change combo selection to drive filter cascade or dialog default
			bool status = cbStatus.SelectedItem is not string selected || selected == All || selected == invoice.StatusDisplay;
			// Exit method early or return value/tuple to caller
			return search && status;
		}

		// Re-run ICollectionView filter and update visible row count label
		private void FilterChanged(object sender, RoutedEventArgs e) => RefreshView();
		private void RefreshView() { _view?.Refresh(); int count = _view?.Cast<object>().Count() ?? 0; lbResultCount.Text = $"{count} hóa đơn"; SetState(count == 0 ? "Không có hóa đơn phù hợp." : string.Empty, count == 0); }

		private void AddInvoice_Click(object sender, RoutedEventArgs e)
		{
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// ContractService.GetContractOptions loads contracts and tenant assignment links
			var (ok, contracts, error) = ManagerUi.TryGet(() => _contractService.GetContractOptions(managerId, _buildingId));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!ok || contracts == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(error ?? "Không thể tải danh sách hợp đồng.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new InvoiceDialog(contracts, managerId, _service) { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				// Call page BLL service CreateInvoice to load or mutate scoped data
				if (ManagerUi.TryRun(() => { _service.CreateInvoice(managerId, dialog.Result); }))
				{
					// Execute UI step inside AddInvoice_Click
					Load();
				}
			}
		}

		private void EditInvoice_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgInvoices.SelectedItem is not InvoiceDto selected) { ShowSelect(); return; }
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service GetInvoice to load or mutate scoped data
			var (formOk, form, formError) = ManagerUi.TryGet(() => _service.GetInvoice(managerId, selected.Id));
			// ContractService.GetContractOptions loads contracts and tenant assignment links
			var (contractsOk, contracts, contractsError) = ManagerUi.TryGet(() => _contractService.GetContractOptions(managerId, _buildingId));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!formOk || !contractsOk || form == null || contracts == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(formError ?? contractsError ?? "Không thể tải dữ liệu hóa đơn.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Guard clause: only continue when UI selection, role, or input is valid
			if (contracts.All(item => item.Id != form.ContractId))
			{
				// Work with BLL DTO/form object returned from service or built from controls
				contracts.Add(new ContractOptionDto { Id = form.ContractId, DisplayName = $"HĐ #{form.ContractId}" });
			}
			// Resolve parent Window so modal dialogs center on the app shell
			var dialog = new InvoiceDialog(contracts, managerId, _service, form) { Owner = Window.GetWindow(this) };
			// Show modal dialog and block until user confirms or cancels
			if (dialog.ShowDialog() == true && dialog.Result != null)
			{
				// Call page BLL service UpdateInvoice to load or mutate scoped data
				if (ManagerUi.TryRun(() => _service.UpdateInvoice(managerId, dialog.Result)))
				{
					// Execute UI step inside EditInvoice_Click
					Load();
				}
			}
		}

		private void DeleteInvoice_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgInvoices.SelectedItem is not InvoiceDto selected) { ShowSelect(); return; }
			// Show Vietnamese MessageBox to confirm, warn, or report success/failure
			if (MessageBox.Show($"Xóa hóa đơn #{selected.Id}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service DeleteInvoice to load or mutate scoped data
			if (ManagerUi.TryRun(() => _service.DeleteInvoice(managerId, selected.Id)))
			{
				// Execute UI step inside DeleteInvoice_Click
				Load();
			}
		}

		private void ExportInvoice_Click(object sender, RoutedEventArgs e)
		{
			// Change combo selection to drive filter cascade or dialog default
			if (dgInvoices.SelectedItem is not InvoiceDto selected)
			{
				// Execute UI step inside ExportInvoice_Click
				ShowSelect();
				// Exit method early or return value/tuple to caller
				return;
			}

			// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
			int managerId = ManagerPageAccess.GetManagerId(_currentUser);
			// Call page BLL service GetInvoice to load or mutate scoped data
			var (ok, invoice, error) = ManagerUi.TryGet(() => _service.GetInvoice(managerId, selected.Id));
			// Guard clause: only continue when UI selection, role, or input is valid
			if (!ok || invoice == null)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(error ?? "Không thể tải hóa đơn.");
				// Exit method early or return value/tuple to caller
				return;
			}
			// Prompt user for invoice CSV filename and folder
			var dialog = new SaveFileDialog
			{
				// Assign local/page state inside ExportInvoice_Click without altering business rules
				Title = "Xuất hóa đơn CSV",
				// Assign local/page state inside ExportInvoice_Click without altering business rules
				Filter = "CSV UTF-8 (*.csv)|*.csv",
				// Assign local/page state inside ExportInvoice_Click without altering business rules
				DefaultExt = ".csv",
				// Assign local/page state inside ExportInvoice_Click without altering business rules
				AddExtension = true,
				// Assign local/page state inside ExportInvoice_Click without altering business rules
				FileName = $"HoaDon_{selected.Id}_{selected.InvoiceDate:yyyy-MM}.csv"
			};
			// Resolve parent Window so modal dialogs center on the app shell
			if (dialog.ShowDialog(Window.GetWindow(this)) != true)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Invoke helper to advance UI workflow without changing logic
				string csv = BuildInvoiceCsv(selected, invoice);
				// Write CSV bytes to disk at user-selected export path
				File.WriteAllText(dialog.FileName, csv, new UTF8Encoding(true));
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(
					// Execute UI step inside ExportInvoice_Click
					$"Đã xuất hóa đơn #{selected.Id} ra file CSV.",
					// Execute UI step inside ExportInvoice_Click
					"Xuất CSV thành công",
					// Execute UI step inside ExportInvoice_Click
					MessageBoxButton.OK,
					// Execute UI step inside ExportInvoice_Click
					MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// ManagerUi.ShowError shows validation/error dialog or wraps BLL exceptions
				ManagerUi.ShowError(ex.Message);
			}
		}

		private static string BuildInvoiceCsv(
			InvoiceDto summary,
			InvoiceFormDto invoice)
		{
			// Assign local/page state inside BuildInvoiceCsv without altering business rules
			var csv = new StringBuilder();
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("HÓA ĐƠN IHOME"));
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("Mã hóa đơn", summary.Id.ToString(CultureInfo.InvariantCulture)));
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("Mã hợp đồng", summary.ContractId.ToString(CultureInfo.InvariantCulture)));
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("Tòa nhà", summary.BuildingName));
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("Phòng", summary.RoomNumber));
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("Người thuê chính", summary.MainTenantName));
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("Ngày lập", summary.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("Hạn thanh toán", summary.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("Trạng thái", summary.StatusDisplay));
			// Execute UI step inside BuildInvoiceCsv
			csv.AppendLine();
			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow("STT", "Khoản thu", "Số lượng", "Đơn giá", "Thành tiền"));

			// Assign local/page state inside BuildInvoiceCsv without altering business rules
			int index = 1;
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var item in invoice.Items)
			{
				// Invoke csv.AppendLine to advance UI workflow without changing logic
				csv.AppendLine(CsvRow(
					// Execute UI step inside BuildInvoiceCsv
					index.ToString(CultureInfo.InvariantCulture),
					// Execute UI step inside BuildInvoiceCsv
					item.Description,
					// Execute UI step inside BuildInvoiceCsv
					item.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
					// Execute UI step inside BuildInvoiceCsv
					item.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture),
					// Execute UI step inside BuildInvoiceCsv
					item.Amount.ToString("0.##", CultureInfo.InvariantCulture)));
				// Execute UI step inside BuildInvoiceCsv
				index++;
			}

			// Invoke csv.AppendLine to advance UI workflow without changing logic
			csv.AppendLine(CsvRow(
				// Execute UI step inside BuildInvoiceCsv
				string.Empty,
				// Execute UI step inside BuildInvoiceCsv
				"TỔNG CỘNG",
				// Execute UI step inside BuildInvoiceCsv
				string.Empty,
				// Execute UI step inside BuildInvoiceCsv
				string.Empty,
				// Execute UI step inside BuildInvoiceCsv
				invoice.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture)));
			// Exit method early or return value/tuple to caller
			return csv.ToString();
		}

		private static string CsvRow(params string?[] values) =>
			string.Join(",", values.Select(EscapeCsvCell));

		private static string EscapeCsvCell(string? value)
		{
			// Assign local/page state inside EscapeCsvCell without altering business rules
			string safeValue = value ?? string.Empty;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (safeValue.Length > 0 && "=+-@\t\r".Contains(safeValue[0]))
			{
				// Assign local/page state inside EscapeCsvCell without altering business rules
				safeValue = $"'{safeValue}";
			}
			// Exit method early or return value/tuple to caller
			return $"\"{safeValue.Replace("\"", "\"\"")}\"";
		}

		private void SetState(string message, bool visible) { lbState.Text = message; brdState.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }
		private static void ShowSelect() => MessageBox.Show("Vui lòng chọn hóa đơn.", "Chưa chọn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
