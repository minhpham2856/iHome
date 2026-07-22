using ClosedXML.Excel;
using iHome.BLL.DTOs;
using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChartPoint = iHome.BLL.DTOs.ChartPoint;

namespace iHome.UI.Views.Landlord
{
	// báo cáo thống kê theo tháng — grid 4 bảng + biểu đồ; xuất Excel qua ClosedXML
	public partial class ReportsPage : Page
	{
		private readonly User _currentUser;
		private readonly LandlordReportService _service = new();
		private bool _suppressFilterEvents;
		// cache báo cáo hiện tại cho nút Export
		private ReportDataDto? _currentReport;

		public ReportsPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += ReportsPage_Loaded;
		}

		private void ReportsPage_Loaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe one-shot Loaded handler after initialization
			Loaded -= ReportsPage_Loaded;
			// Execute UI step inside ReportsPage_Loaded
			InitFilters();
		}

		private PropertyFilterOptionDto? SelectedProperty => cbProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cbBuilding.SelectedItem as BuildingFilterOptionDto;
		private ReportMonthOptionDto? SelectedMonth => cbMonth.SelectedItem as ReportMonthOptionDto;

		// bind property + tháng; building load sau qua LoadBuildingsAndReport
		private void InitFilters()
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = true;
				// Call page BLL service GetPropertyOptions to load or mutate scoped data
				cbProperty.ItemsSource = _service.GetPropertyOptions(_currentUser.Id);
				// Pick default combo index (usually first/all option) after reload
				cbProperty.SelectedIndex = 0;
				// Call page BLL service GetMonthOptions to load or mutate scoped data
				cbMonth.ItemsSource = _service.GetMonthOptions();
				// Pick default combo index (usually first/all option) after reload
				cbMonth.SelectedIndex = 0;
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Execute UI step inside InitFilters
				LoadBuildingsAndReport();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể tải bộ lọc", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// đổi property → reload combo tòa rồi LoadReport
		private void LoadBuildingsAndReport()
		{
			// Assign local/page state inside LoadBuildingsAndReport without altering business rules
			int propertyId = SelectedProperty?.Id ?? 0;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = true;
				// Call page BLL service GetBuildingOptions to load or mutate scoped data
				var buildings = _service.GetBuildingOptions(_currentUser.Id, propertyId);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				cbBuilding.ItemsSource = buildings;
				// Pick default combo index (usually first/all option) after reload
				cbBuilding.SelectedIndex = buildings.Count > 0 ? 0 : -1;
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Execute UI step inside LoadBuildingsAndReport
				LoadReport();
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_suppressFilterEvents = false;
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể tải tòa nhà", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// aggregate một tháng theo property/building filter
		private void LoadReport()
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (SelectedMonth == null) return;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetReport to load or mutate scoped data
				_currentReport = _service.GetReport(
					// Execute UI step inside LoadReport
					_currentUser.Id,
					// Execute UI step inside LoadReport
					SelectedProperty?.Id ?? 0,
					// Execute UI step inside LoadReport
					SelectedBuilding?.Id ?? 0,
					// Execute UI step inside LoadReport
					SelectedMonth.Year,
					// Execute UI step inside LoadReport
					SelectedMonth.Month);
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgRooms.ItemsSource = _currentReport.Rooms;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgRevenues.ItemsSource = _currentReport.Revenues;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgInvoices.ItemsSource = _currentReport.Invoices;
				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				dgContracts.ItemsSource = _currentReport.Contracts;
				// Call helper BindCharts to refresh UI state from BLL data
				BindCharts(_currentReport);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể tải báo cáo", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// cột doanh thu theo ngày + pie trạng thái phòng
		private void BindCharts(ReportDataDto data)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbRevenueChartTitle.Text = $"Doanh thu theo ngày ({data.PeriodLabel})";

			// Configure LiveCharts series/axes from dashboard DTO chart points
			chartRevenue.Series = new SeriesCollection
			{
				// Configure LiveCharts series/axes from dashboard DTO chart points
				new ColumnSeries
				{
					// Assign local/page state inside BindCharts without altering business rules
					Title = "Doanh thu",
					// Configure LiveCharts series/axes from dashboard DTO chart points
					Values = new ChartValues<double>(data.RevenueSeries.ConvertAll(p => p.Value)),
					// Assign local/page state inside BindCharts without altering business rules
					Fill = new SolidColorBrush(Color.FromRgb(44, 62, 80))
				}
			};
			// Configure LiveCharts series/axes from dashboard DTO chart points
			chartRevenue.AxisX = new AxesCollection
			{
				// Configure LiveCharts series/axes from dashboard DTO chart points
				new Axis
				{
					// Configure LiveCharts series/axes from dashboard DTO chart points
					Labels = data.RevenueSeries.ConvertAll(p => p.Label).ToArray(),
					// Configure LiveCharts series/axes from dashboard DTO chart points
					LabelsRotation = data.RevenueSeries.Count > 16 ? 45 : 0
				}
			};

			// Configure LiveCharts series/axes from dashboard DTO chart points
			double maxRevenue = data.RevenueSeries.Count == 0 ? 0 : data.RevenueSeries.Max(p => p.Value);
			// Configure LiveCharts series/axes from dashboard DTO chart points
			chartRevenue.AxisY = new AxesCollection
			{
				// Configure LiveCharts series/axes from dashboard DTO chart points
				new Axis
				{
					// Assign local/page state inside BindCharts without altering business rules
					MinValue = 0,
					// Assign local/page state inside BindCharts without altering business rules
					MaxValue = maxRevenue <= 0 ? 1 : double.NaN,
					// Assign local/page state inside BindCharts without altering business rules
					LabelFormatter = value => Math.Round(value, 0, MidpointRounding.AwayFromZero)
						// Execute UI step inside BindCharts
						.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))
				}
			};

			// Invoke helper to advance UI workflow without changing logic
			chartRoomStatus.Series = BuildPie(
				// Configure LiveCharts series/axes from dashboard DTO chart points
				data.RoomStatusSeries,
				// Execute UI step inside BindCharts
				Color.FromRgb(46, 204, 113),
				// Execute UI step inside BindCharts
				Color.FromRgb(149, 165, 166),
				// Execute UI step inside BindCharts
				Color.FromRgb(52, 152, 219),
				// Execute UI step inside BindCharts
				Color.FromRgb(241, 196, 15));
		}

		private static SeriesCollection BuildPie(List<ChartPoint> points, params Color[] colors)
		{
			// Configure LiveCharts series/axes from dashboard DTO chart points
			var series = new SeriesCollection();
			// Iterate collection to update UI, chart series, or CSV rows
			for (int i = 0; i < points.Count; i++)
			{
				// Configure LiveCharts series/axes from dashboard DTO chart points
				series.Add(new PieSeries
				{
					// Assign local/page state inside BuildPie without altering business rules
					Title = points[i].Label,
					// Configure LiveCharts series/axes from dashboard DTO chart points
					Values = new ChartValues<double> { points[i].Value },
					// Assign local/page state inside BuildPie without altering business rules
					Fill = new SolidColorBrush(colors[i % colors.Length]),
					// Assign local/page state inside BuildPie without altering business rules
					DataLabels = true,
					// Assign local/page state inside BuildPie without altering business rules
					LabelPoint = chartPoint => chartPoint.Y.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))
				// Execute UI step inside BuildPie
				});
			}
			// Exit method early or return value/tuple to caller
			return series;
		}

		// property đổi → cascade buildings; building/tháng đổi → chỉ reload report
		private void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_suppressFilterEvents || !IsLoaded) return;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (ReferenceEquals(sender, cbProperty))
			{
				// Execute UI step inside FilterChanged
				LoadBuildingsAndReport();
				// Exit method early or return value/tuple to caller
				return;
			}
			// Execute UI step inside FilterChanged
			LoadReport();
		}

		// Assign local/page state inside btnRefresh_Click without altering business rules
		private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadReport();

		// xuất 4 sheet Excel từ _currentReport
		private void btnExportExcel_Click(object sender, RoutedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_currentReport == null)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Chưa có dữ liệu báo cáo để xuất.", "Xuất Excel", MessageBoxButton.OK, MessageBoxImage.Information);
				// Exit method early or return value/tuple to caller
				return;
			}

			// Prompt user for invoice CSV filename and folder
			var dialog = new SaveFileDialog
			{
				// Assign local/page state inside btnExportExcel_Click without altering business rules
				Title = "Xuất báo cáo Excel",
				// Assign local/page state inside btnExportExcel_Click without altering business rules
				Filter = "Excel (*.xlsx)|*.xlsx",
				// Assign local/page state inside btnExportExcel_Click without altering business rules
				DefaultExt = ".xlsx",
				// Assign local/page state inside btnExportExcel_Click without altering business rules
				AddExtension = true,
				// Assign local/page state inside btnExportExcel_Click without altering business rules
				FileName = $"BaoCao_{_currentReport.PeriodLabel.Replace('/', '-')}.xlsx"
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
				using var workbook = new XLWorkbook();
				// Execute UI step inside btnExportExcel_Click
				WriteSheet(workbook, "Phong",
					// Execute UI step inside btnExportExcel_Click
					new[] { "Nhà trọ", "Tòa", "Phòng", "Tầng", "Loại", "Trạng thái", "Giá thuê" },
					// LINQ step to shape in-memory list for filters, KPIs, or binding
					_currentReport.Rooms.Select(r => new object[]
					{
						// Execute UI step inside btnExportExcel_Click
						r.PropertyName, r.BuildingName, r.RoomNumber, r.Floor, r.RoomTypeName, r.StatusDisplay, r.BaseRent
					// Execute UI step inside btnExportExcel_Click
					}));
				// Execute UI step inside btnExportExcel_Click
				WriteSheet(workbook, "DoanhThu",
					// Execute UI step inside btnExportExcel_Click
					new[] { "Ngày", "Tòa", "Phòng", "Số tiền", "Phương thức" },
					// LINQ step to shape in-memory list for filters, KPIs, or binding
					_currentReport.Revenues.Select(r => new object[]
					{
						// Execute UI step inside btnExportExcel_Click
						r.PaymentDateDisplay, r.BuildingName, r.RoomNumber, r.Amount, r.MethodDisplay
					// Execute UI step inside btnExportExcel_Click
					}));
				// Execute UI step inside btnExportExcel_Click
				WriteSheet(workbook, "HoaDon",
					// Execute UI step inside btnExportExcel_Click
					new[] { "Ngày lập", "Tòa", "Phòng", "Tổng tiền", "Trạng thái", "Hạn TT" },
					// LINQ step to shape in-memory list for filters, KPIs, or binding
					_currentReport.Invoices.Select(r => new object[]
					{
						// Execute UI step inside btnExportExcel_Click
						r.InvoiceDateDisplay, r.BuildingName, r.RoomNumber, r.TotalAmount, r.StatusDisplay, r.DueDateDisplay
					// Execute UI step inside btnExportExcel_Click
					}));
				// Execute UI step inside btnExportExcel_Click
				WriteSheet(workbook, "HopDong",
					// Execute UI step inside btnExportExcel_Click
					new[] { "Tòa", "Phòng", "Khách chính", "Bắt đầu", "Kết thúc", "Tiền thuê", "Trạng thái" },
					// LINQ step to shape in-memory list for filters, KPIs, or binding
					_currentReport.Contracts.Select(r => new object[]
					{
						// Execute UI step inside btnExportExcel_Click
						r.BuildingName, r.RoomNumber, r.MainTenantName, r.StartDateDisplay, r.EndDateDisplay, r.MonthlyRent, r.StatusDisplay
					// Execute UI step inside btnExportExcel_Click
					}));
				// Execute UI step inside btnExportExcel_Click
				workbook.SaveAs(dialog.FileName);
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show("Đã xuất báo cáo ra file Excel.", "Xuất Excel thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể xuất Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// helper — header row bold + auto-fit cột
		private static void WriteSheet(
			XLWorkbook workbook,
			string sheetName,
			IReadOnlyList<string> headers,
			IEnumerable<object[]> rows)
		{
			// Assign local/page state inside WriteSheet without altering business rules
			var sheet = workbook.Worksheets.Add(sheetName);
			// Iterate collection to update UI, chart series, or CSV rows
			for (int c = 0; c < headers.Count; c++)
			{
				// Assign local/page state inside WriteSheet without altering business rules
				sheet.Cell(1, c + 1).Value = headers[c];
			}
			// Assign local/page state inside WriteSheet without altering business rules
			sheet.Row(1).Style.Font.Bold = true;

			// Assign local/page state inside WriteSheet without altering business rules
			int rowIndex = 2;
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (object[] row in rows)
			{
				// Iterate collection to update UI, chart series, or CSV rows
				for (int c = 0; c < row.Length; c++)
				{
					// Assign local/page state inside WriteSheet without altering business rules
					var cell = sheet.Cell(rowIndex, c + 1);
					// Assign local/page state inside WriteSheet without altering business rules
					object value = row[c];
					// Guard clause: only continue when UI selection, role, or input is valid
					if (value is decimal d)
					{
						// Assign local/page state inside WriteSheet without altering business rules
						cell.Value = d;
					}
					// Alternate branch when previous condition was not satisfied
					else if (value is int i)
					{
						// Assign local/page state inside WriteSheet without altering business rules
						cell.Value = i;
					}
					// Alternate branch when previous condition was not satisfied
					else
					{
						// Assign local/page state inside WriteSheet without altering business rules
						cell.Value = value?.ToString() ?? string.Empty;
					}
				}
				// Execute UI step inside WriteSheet
				rowIndex++;
			}

			// Guard clause: only continue when UI selection, role, or input is valid
			if (sheet.Columns().Any())
			{
				// Execute UI step inside WriteSheet
				sheet.Columns().AdjustToContents();
			}
		}
	}
}
