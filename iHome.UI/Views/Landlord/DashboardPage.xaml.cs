using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iHome.BLL.DTOs;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using LiveCharts;
using LiveCharts.Wpf;
using ChartPoint = iHome.BLL.DTOs.ChartPoint;

namespace iHome.UI.Views.Landlord
{
	// bảng điều khiển chủ trọ — KPI cards + biểu đồ doanh thu/trạng thái phòng-hợp đồng; filter theo nhà trọ và khoảng thời gian
	public partial class DashboardPage : Page
	{
		private readonly User _currentUser;
		private readonly DashboardService _service = new();
		// cờ tránh ReloadDashboard khi đang set SelectedIndex/DatePicker lúc InitFilters hoặc preset
		private bool _isLoadingFilters;

		// item nội bộ cho ComboBox preset doanh thu (key gửi xuống GetRevenueRange)
		private sealed class RevenueRangeOption
		{
			public string Key { get; set; } = string.Empty;
			public string Name { get; set; } = string.Empty;
		}

		public DashboardPage(User user)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Throw when role guard or required theme resource is missing
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += DashboardPage_Loaded;
		}

		// một lần Loaded — gỡ handler rồi init filter và load data
		private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
		{
			// Unsubscribe one-shot Loaded handler after initialization
			Loaded -= DashboardPage_Loaded;
			// Execute UI step inside DashboardPage_Loaded
			InitFilters();
			// Call helper ReloadDashboard to refresh UI state from BLL data
			ReloadDashboard();
		}

		// bind combo nhà trọ + preset thời gian; mặc định "Tất cả" và sync DatePicker
		private void InitFilters()
		{
			// Flip internal flag to suppress duplicate events or mark in-flight operation
			_isLoadingFilters = true;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Call page BLL service GetPropertyOptions to load or mutate scoped data
				cbProperty.ItemsSource = _service.GetPropertyOptions(_currentUser.Id);
				// Pick default combo index (usually first/all option) after reload
				cbProperty.SelectedIndex = 0;

				// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
				cbRevenueRange.ItemsSource = new List<RevenueRangeOption>
				{
					// Assign local/page state inside InitFilters without altering business rules
					new() { Key = "all", Name = "Tất cả" },
					// Assign local/page state inside InitFilters without altering business rules
					new() { Key = "year", Name = "Năm nay" },
					// Assign local/page state inside InitFilters without altering business rules
					new() { Key = "6m", Name = "6 tháng nay" },
					// Assign local/page state inside InitFilters without altering business rules
					new() { Key = "month", Name = "Tháng này" },
					// Assign local/page state inside InitFilters without altering business rules
					new() { Key = "week", Name = "Tuần này" },
					// Assign local/page state inside InitFilters without altering business rules
					new() { Key = "custom", Name = "Tùy chọn" }
				};
				// Pick default combo index (usually first/all option) after reload
				cbRevenueRange.SelectedIndex = 0;
				// Call helper ApplyRevenuePreset to refresh UI state from BLL data
				ApplyRevenuePreset("all", syncPickers: true);
			}
			// Always reset loading flags and re-enable refresh regardless of success
			finally
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoadingFilters = false;
			}
		}

		// null Id = tất cả nhà trọ của landlord
		private int? SelectedPropertyId =>
			(cbProperty.SelectedItem as DashboardPropertyOption)?.Id;

		// gọi BLL aggregate rồi bind cards + charts + tiêu đề khoảng doanh thu
		private void ReloadDashboard()
		{
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Invoke helper to advance UI workflow without changing logic
				var (from, to) = GetRevenueRange();
				// Call page BLL service GetDashboardData to load or mutate scoped data
				var data = _service.GetDashboardData(_currentUser.Id, SelectedPropertyId, from, to);
				// Call helper BindCards to refresh UI state from BLL data
				BindCards(data);
				// Call helper BindCharts to refresh UI state from BLL data
				BindCharts(data);

				// Assign local/page state inside ReloadDashboard without altering business rules
				string rangeText = from.HasValue
					// Execute UI step inside ReloadDashboard
					? $"{from.Value:dd/MM/yyyy} – {to:dd/MM/yyyy}"
					// Execute UI step inside ReloadDashboard
					: $"đến {to:dd/MM/yyyy}";
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbRevenueTitle.Text = $"Doanh thu ({rangeText})";
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception ex)
			{
				// Show Vietnamese MessageBox to confirm, warn, or report success/failure
				MessageBox.Show(ex.Message, "Không thể tải dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// gán text KPI lên các label trên lưới card
		private void BindCards(DashboardData data)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotalProperties.Text = data.TotalProperties.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotalBuildings.Text = data.TotalBuildings.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotalRooms.Text = data.TotalRooms.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOccupiedRooms.Text = data.OccupiedRooms.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbVacantRooms.Text = data.VacantRooms.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTotalTenants.Text = data.TotalTenants.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbActiveContracts.Text = data.ActiveContracts.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbMonthlyRevenue.Text = data.MonthlyRevenue.ToString("N0");
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOutstanding.Text = data.OutstandingAmount.ToString("N0");
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOverdue.Text = data.OverdueInvoices.ToString();
		}

		// LiveCharts2 — cột doanh thu theo tháng/ngày + 2 pie trạng thái phòng và hợp đồng
		private void BindCharts(DashboardData data)
		{
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
					LabelsRotation = data.RevenueSeries.Count > 14 ? 45 : 0
				}
			};
			// Configure LiveCharts series/axes from dashboard DTO chart points
			chartRevenue.AxisY = new AxesCollection
			{
				// Configure LiveCharts series/axes from dashboard DTO chart points
				new Axis
				{
					// Assign local/page state inside BindCharts without altering business rules
					MinValue = 0,
					// Assign local/page state inside BindCharts without altering business rules
					LabelFormatter = value => Math.Round(value, 0, MidpointRounding.AwayFromZero)
						// Execute UI step inside BindCharts
						.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))
				}
			};

			// Invoke helper to advance UI workflow without changing logic
			chartRoomStatus.Series = BuildPie(data.RoomStatusSeries,
				// Execute UI step inside BindCharts
				Color.FromRgb(46, 204, 113), Color.FromRgb(149, 165, 166),
				// Execute UI step inside BindCharts
				Color.FromRgb(52, 152, 219), Color.FromRgb(241, 196, 15));

			// Invoke helper to advance UI workflow without changing logic
			chartContractStatus.Series = BuildPie(data.ContractStatusSeries,
				// Execute UI step inside BindCharts
				Color.FromRgb(46, 204, 113), Color.FromRgb(231, 76, 60), Color.FromRgb(149, 165, 166));
		}

		// helper pie — mỗi slice một PieSeries (LiveCharts pattern)
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
					DataLabels = true
				// Execute UI step inside BuildPie
				});
			}
			// Exit method early or return value/tuple to caller
			return series;
		}

		// map UI filter → (from?, to) gửi DashboardService; "all" = from null
		private (DateOnly? from, DateOnly to) GetRevenueRange()
		{
			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			string key = (cbRevenueRange.SelectedItem as RevenueRangeOption)?.Key ?? "custom";

			if (key == "all")
			{
				return (null, today);
			}

			DateOnly from = dpRevenueFrom.SelectedDate.HasValue
				? DateOnly.FromDateTime(dpRevenueFrom.SelectedDate.Value.Date)
				: today;
			DateOnly to = dpRevenueTo.SelectedDate.HasValue
				? DateOnly.FromDateTime(dpRevenueTo.SelectedDate.Value.Date)
				: today;
			if (to < from)
			{
				(from, to) = (to, from);
			}
			return (from, to);
		}

		// tính ngày bắt đầu theo preset; syncPickers=true thì ghi vào DatePicker (tắt event tạm)
		private void ApplyRevenuePreset(string key, bool syncPickers)
		{
			// Convert between DatePicker DateTime and DateOnly DTO fields
			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			// Convert between DatePicker DateTime and DateOnly DTO fields
			DateOnly from;
			// Execute UI step inside ApplyRevenuePreset
			switch (key)
			{
				// Execute UI step inside ApplyRevenuePreset
				case "year":
					// Convert between DatePicker DateTime and DateOnly DTO fields
					from = new DateOnly(today.Year, 1, 1);
					break;
				// Execute UI step inside ApplyRevenuePreset
				case "6m":
					// Convert between DatePicker DateTime and DateOnly DTO fields
					from = new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
					break;
				// Execute UI step inside ApplyRevenuePreset
				case "month":
					// Convert between DatePicker DateTime and DateOnly DTO fields
					from = new DateOnly(today.Year, today.Month, 1);
					break;
				// Execute UI step inside ApplyRevenuePreset
				case "week":
					// Assign local/page state inside ApplyRevenuePreset without altering business rules
					int offset = ((int)today.DayOfWeek + 6) % 7;
					// Assign local/page state inside ApplyRevenuePreset without altering business rules
					from = today.AddDays(-offset);
					break;
				// Execute UI step inside ApplyRevenuePreset
				case "all":
					// Assign local/page state inside ApplyRevenuePreset without altering business rules
					from = today.AddYears(-10);
					break;
				// Execute UI step inside ApplyRevenuePreset
				default:
					// Read/write DatePicker for contract, invoice, or birth date fields
					from = dpRevenueFrom.SelectedDate.HasValue
						// Read/write DatePicker for contract, invoice, or birth date fields
						? DateOnly.FromDateTime(dpRevenueFrom.SelectedDate.Value.Date)
						// Execute UI step inside ApplyRevenuePreset
						: today.AddMonths(-1);
					break;
			}

			// Guard clause: only continue when UI selection, role, or input is valid
			if (syncPickers)
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoadingFilters = true;
				// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
				try
				{
					// Read/write DatePicker for contract, invoice, or birth date fields
					dpRevenueFrom.SelectedDate = from.ToDateTime(TimeOnly.MinValue);
					// Read/write DatePicker for contract, invoice, or birth date fields
					dpRevenueTo.SelectedDate = today.ToDateTime(TimeOnly.MinValue);
				}
				// Always reset loading flags and re-enable refresh regardless of success
				finally
				{
					// Flip internal flag to suppress duplicate events or mark in-flight operation
					_isLoadingFilters = false;
				}
			}
		}

		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isLoadingFilters || !IsLoaded) return;
			// Call helper ReloadDashboard to refresh UI state from BLL data
			ReloadDashboard();
		}

		private void cbRevenueRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isLoadingFilters || !IsLoaded) return;
			// Change combo selection to drive filter cascade or dialog default
			if (cbRevenueRange.SelectedItem is not RevenueRangeOption option) return;
			// Guard clause: only continue when UI selection, role, or input is valid
			if (option.Key != "custom")
			{
				// Call helper ApplyRevenuePreset to refresh UI state from BLL data
				ApplyRevenuePreset(option.Key, syncPickers: true);
			}
			// Call helper ReloadDashboard to refresh UI state from BLL data
			ReloadDashboard();
		}

		private void dpRevenue_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isLoadingFilters || !IsLoaded) return;

			// người dùng chỉnh tay → chuyển sang Tùy chọn
			_isLoadingFilters = true;
			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Iterate collection to update UI, chart series, or CSV rows
				foreach (RevenueRangeOption item in cbRevenueRange.Items)
				{
					// Guard clause: only continue when UI selection, role, or input is valid
					if (item.Key == "custom")
					{
						// Restore or set combo selection to match entity id or filter
						cbRevenueRange.SelectedItem = item;
						break;
					}
				}
			}
			// Always reset loading flags and re-enable refresh regardless of success
			finally
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoadingFilters = false;
			}

			// Call helper ReloadDashboard to refresh UI state from BLL data
			ReloadDashboard();
		}
	}
}
