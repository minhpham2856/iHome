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
	// Monthly reports — four grids + charts; Excel export via ClosedXML
	public partial class ReportsPage : Page
	{
		private readonly User _currentUser;
		private readonly LandlordReportService _service = new();
		private bool _suppressFilterEvents;
		// Cached report for Export button
		private ReportDataDto? _currentReport;

		public ReportsPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			Loaded += ReportsPage_Loaded;
		}

		private void ReportsPage_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= ReportsPage_Loaded;
			InitFilters();
		}

		private PropertyFilterOptionDto? SelectedProperty => cbProperty.SelectedItem as PropertyFilterOptionDto;
		private BuildingFilterOptionDto? SelectedBuilding => cbBuilding.SelectedItem as BuildingFilterOptionDto;
		private ReportMonthOptionDto? SelectedMonth => cbMonth.SelectedItem as ReportMonthOptionDto;

		// Bind property + month; buildings load via LoadBuildingsAndReport
		private void InitFilters()
		{
			try
			{
				_suppressFilterEvents = true;
				cbProperty.ItemsSource = _service.GetPropertyOptions(_currentUser.Id);
				cbProperty.SelectedIndex = 0;
				cbMonth.ItemsSource = _service.GetMonthOptions();
				cbMonth.SelectedIndex = 0;
				_suppressFilterEvents = false;
				LoadBuildingsAndReport();
			}
			catch (Exception ex)
			{
				_suppressFilterEvents = false;
				MessageBox.Show(ex.Message, "Không thể tải bộ lọc", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Property change → reload building combo then report
		private void LoadBuildingsAndReport()
		{
			int propertyId = SelectedProperty?.Id ?? 0;
			try
			{
				_suppressFilterEvents = true;
				var buildings = _service.GetBuildingOptions(_currentUser.Id, propertyId);
				cbBuilding.ItemsSource = buildings;
				cbBuilding.SelectedIndex = buildings.Count > 0 ? 0 : -1;
				_suppressFilterEvents = false;
				LoadReport();
			}
			catch (Exception ex)
			{
				_suppressFilterEvents = false;
				MessageBox.Show(ex.Message, "Không thể tải tòa nhà", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Aggregate one month for current property/building filters
		private void LoadReport()
		{
			if (SelectedMonth == null) return;
			try
			{
				_currentReport = _service.GetReport(
					_currentUser.Id,
					SelectedProperty?.Id ?? 0,
					SelectedBuilding?.Id ?? 0,
					SelectedMonth.Year,
					SelectedMonth.Month);
				dgRooms.ItemsSource = _currentReport.Rooms;
				dgRevenues.ItemsSource = _currentReport.Revenues;
				dgInvoices.ItemsSource = _currentReport.Invoices;
				dgContracts.ItemsSource = _currentReport.Contracts;
				BindCharts(_currentReport);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể tải báo cáo", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// LiveCharts: daily revenue columns + room-status pie
		private void BindCharts(ReportDataDto data)
		{
			lbRevenueChartTitle.Text = $"Doanh thu theo ngày ({data.PeriodLabel})";

			chartRevenue.Series = new SeriesCollection
			{
				new ColumnSeries
				{
					Title = "Doanh thu",
					Values = new ChartValues<double>(data.RevenueSeries.ConvertAll(p => p.Value)),
					Fill = new SolidColorBrush(Color.FromRgb(44, 62, 80))
				}
			};
			chartRevenue.AxisX = new AxesCollection
			{
				new Axis
				{
					Labels = data.RevenueSeries.ConvertAll(p => p.Label).ToArray(),
					LabelsRotation = data.RevenueSeries.Count > 16 ? 45 : 0
				}
			};

			// MaxValue NaN lets LiveCharts auto-scale; use 1 when all zeros so axis stays valid
			double maxRevenue = data.RevenueSeries.Count == 0 ? 0 : data.RevenueSeries.Max(p => p.Value);
			chartRevenue.AxisY = new AxesCollection
			{
				new Axis
				{
					MinValue = 0,
					MaxValue = maxRevenue <= 0 ? 1 : double.NaN,
					LabelFormatter = value => Math.Round(value, 0, MidpointRounding.AwayFromZero)
						.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))
				}
			};

			chartRoomStatus.Series = BuildPie(
				data.RoomStatusSeries,
				Color.FromRgb(46, 204, 113),
				Color.FromRgb(149, 165, 166),
				Color.FromRgb(52, 152, 219),
				Color.FromRgb(241, 196, 15));
		}

		// Build pie series from chart points
		private static SeriesCollection BuildPie(List<ChartPoint> points, params Color[] colors)
		{
			var series = new SeriesCollection();
			for (int i = 0; i < points.Count; i++)
			{
				series.Add(new PieSeries
				{
					Title = points[i].Label,
					Values = new ChartValues<double> { points[i].Value },
					Fill = new SolidColorBrush(colors[i % colors.Length]),
					DataLabels = true,
					LabelPoint = chartPoint => chartPoint.Y.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))
				});
			}
			return series;
		}

		// Property → cascade buildings; building/month → reload report only
		private void FilterChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suppressFilterEvents || !IsLoaded) return;
			if (ReferenceEquals(sender, cbProperty))
			{
				LoadBuildingsAndReport();
				return;
			}
			LoadReport();
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadReport();

		// ClosedXML: four sheets from _currentReport
		private void btnExportExcel_Click(object sender, RoutedEventArgs e)
		{
			if (_currentReport == null)
			{
				MessageBox.Show("Chưa có dữ liệu báo cáo để xuất.", "Xuất Excel", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var dialog = new SaveFileDialog
			{
				Title = "Xuất báo cáo Excel",
				Filter = "Excel (*.xlsx)|*.xlsx",
				DefaultExt = ".xlsx",
				AddExtension = true,
				FileName = $"BaoCao_{_currentReport.PeriodLabel.Replace('/', '-')}.xlsx"
			};
			if (dialog.ShowDialog(Window.GetWindow(this)) != true)
			{
				return;
			}

			try
			{
				using var workbook = new XLWorkbook();
				WriteSheet(workbook, "Phong",
					new[] { "Nhà trọ", "Tòa", "Phòng", "Tầng", "Loại", "Trạng thái", "Giá thuê" },
					_currentReport.Rooms.Select(r => new object[]
					{
						r.PropertyName, r.BuildingName, r.RoomNumber, r.Floor, r.RoomTypeName, r.StatusDisplay, r.BaseRent
					}));
				WriteSheet(workbook, "DoanhThu",
					new[] { "Ngày", "Tòa", "Phòng", "Số tiền", "Phương thức" },
					_currentReport.Revenues.Select(r => new object[]
					{
						r.PaymentDateDisplay, r.BuildingName, r.RoomNumber, r.Amount, r.MethodDisplay
					}));
				WriteSheet(workbook, "HoaDon",
					new[] { "Ngày lập", "Tòa", "Phòng", "Tổng tiền", "Trạng thái", "Hạn TT" },
					_currentReport.Invoices.Select(r => new object[]
					{
						r.InvoiceDateDisplay, r.BuildingName, r.RoomNumber, r.TotalAmount, r.StatusDisplay, r.DueDateDisplay
					}));
				WriteSheet(workbook, "HopDong",
					new[] { "Tòa", "Phòng", "Khách chính", "Bắt đầu", "Kết thúc", "Tiền thuê", "Trạng thái" },
					_currentReport.Contracts.Select(r => new object[]
					{
						r.BuildingName, r.RoomNumber, r.MainTenantName, r.StartDateDisplay, r.EndDateDisplay, r.MonthlyRent, r.StatusDisplay
					}));
				workbook.SaveAs(dialog.FileName);
				MessageBox.Show("Đã xuất báo cáo ra file Excel.", "Xuất Excel thành công", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể xuất Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// ClosedXML helper — bold header row, typed cells, auto-fit columns
		private static void WriteSheet(
			XLWorkbook workbook,
			string sheetName,
			IReadOnlyList<string> headers,
			IEnumerable<object[]> rows)
		{
			var sheet = workbook.Worksheets.Add(sheetName);
			for (int c = 0; c < headers.Count; c++)
			{
				sheet.Cell(1, c + 1).Value = headers[c];
			}
			sheet.Row(1).Style.Font.Bold = true;

			int rowIndex = 2;
			foreach (object[] row in rows)
			{
				for (int c = 0; c < row.Length; c++)
				{
					var cell = sheet.Cell(rowIndex, c + 1);
					object value = row[c];
					if (value is decimal d)
					{
						cell.Value = d;
					}
					else if (value is int i)
					{
						cell.Value = i;
					}
					else
					{
						cell.Value = value?.ToString() ?? string.Empty;
					}
				}
				rowIndex++;
			}

			if (sheet.Columns().Any())
			{
				sheet.Columns().AdjustToContents();
			}
		}
	}
}
