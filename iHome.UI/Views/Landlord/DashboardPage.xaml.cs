using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using iHome.BLL.DTOs;
using iHome.BLL.Services;
using iHome.DAL.Entities;
using LiveCharts;
using LiveCharts.Wpf;
using ChartPoint = iHome.BLL.DTOs.ChartPoint;

namespace iHome.UI.Views.Landlord
{
	// Landlord dashboard — KPI cards, revenue/room/contract charts, property + date filters
	public partial class DashboardPage : Page
	{
		private readonly User _currentUser;
		private readonly DashboardService _service = new();
		// Suppresses ReloadDashboard while InitFilters / presets write combo and DatePicker values
		private bool _isLoadingFilters;

		// Combo item for revenue range preset (Key maps to GetRevenueRange)
		private sealed class RevenueRangeOption
		{
			public string Key { get; set; } = string.Empty;
			public string Name { get; set; } = string.Empty;
		}

		public DashboardPage(User user)
		{
			InitializeComponent();
			_currentUser = user ?? throw new ArgumentNullException(nameof(user));
			Loaded += DashboardPage_Loaded;
		}

		// One-shot init: filters then first data load
		private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= DashboardPage_Loaded;
			InitFilters();
			ReloadDashboard();
		}

		// Bind property combo + revenue presets; default "all" and sync DatePickers
		private void InitFilters()
		{
			_isLoadingFilters = true;
			try
			{
				cbProperty.ItemsSource = _service.GetPropertyOptions(_currentUser.Id);
				cbProperty.SelectedIndex = 0;

				cbRevenueRange.ItemsSource = new List<RevenueRangeOption>
				{
					new() { Key = "all", Name = "Tất cả" },
					new() { Key = "year", Name = "Năm nay" },
					new() { Key = "6m", Name = "6 tháng nay" },
					new() { Key = "month", Name = "Tháng này" },
					new() { Key = "week", Name = "Tuần này" },
					new() { Key = "custom", Name = "Tùy chọn" }
				};
				cbRevenueRange.SelectedIndex = 0;
				ApplyRevenuePreset("all", syncPickers: true);
			}
			finally
			{
				_isLoadingFilters = false;
			}
		}

		// null Id = all properties for this landlord
		private int? SelectedPropertyId =>
			(cbProperty.SelectedItem as DashboardPropertyOption)?.Id;

		// Load aggregates and bind cards, charts, and revenue title
		private void ReloadDashboard()
		{
			try
			{
				var (from, to) = GetRevenueRange();
				var data = _service.GetDashboardData(_currentUser.Id, SelectedPropertyId, from, to);
				BindCards(data);
				BindCharts(data);

				string rangeText = from.HasValue
					? $"{from.Value:dd/MM/yyyy} – {to:dd/MM/yyyy}"
					: $"đến {to:dd/MM/yyyy}";
				lbRevenueTitle.Text = $"Doanh thu ({rangeText})";
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể tải dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Push KPI values onto card labels
		private void BindCards(DashboardData data)
		{
			lbTotalProperties.Text = data.TotalProperties.ToString();
			lbTotalBuildings.Text = data.TotalBuildings.ToString();
			lbTotalRooms.Text = data.TotalRooms.ToString();
			lbOccupiedRooms.Text = data.OccupiedRooms.ToString();
			lbVacantRooms.Text = data.VacantRooms.ToString();
			lbTotalTenants.Text = data.TotalTenants.ToString();
			lbActiveContracts.Text = data.ActiveContracts.ToString();
			lbMonthlyRevenue.Text = data.MonthlyRevenue.ToString("N0");
			lbOutstanding.Text = data.OutstandingAmount.ToString("N0");
			lbOverdue.Text = data.OverdueInvoices.ToString();
		}

		// LiveCharts: revenue columns + room/contract pie charts
		private void BindCharts(DashboardData data)
		{
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
					LabelsRotation = data.RevenueSeries.Count > 14 ? 45 : 0
				}
			};
			chartRevenue.AxisY = new AxesCollection
			{
				new Axis
				{
					MinValue = 0,
					LabelFormatter = value => Math.Round(value, 0, MidpointRounding.AwayFromZero)
						.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))
				}
			};

			// Pie charts: mutate existing SeriesCollection + redraw after layout (LiveCharts 0.9.7 quirk)
			ApplyPie(chartRoomStatus, data.RoomStatusSeries,
				Color.FromRgb(46, 204, 113), Color.FromRgb(149, 165, 166),
				Color.FromRgb(52, 152, 219), Color.FromRgb(241, 196, 15));
			ApplyPie(chartContractStatus, data.ContractStatusSeries,
				Color.FromRgb(46, 204, 113), Color.FromRgb(231, 76, 60), Color.FromRgb(149, 165, 166));
		}

		// Fill pie series; force Update once ActualWidth is known (LiveCharts layout timing)
		private void ApplyPie(PieChart chart, List<ChartPoint> points, params Color[] colors)
		{
			chart.DisableAnimations = true;
			if (chart.Series == null)
			{
				chart.Series = new SeriesCollection();
			}
			else
			{
				chart.Series.Clear();
			}

			var slices = BuildPieSlices(points, colors);
			if (slices.Count == 0)
			{
				slices.Add(new PieSeries
				{
					Title = "Không có dữ liệu",
					Values = new ChartValues<double> { 1 },
					Fill = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
					StrokeThickness = 0,
					DataLabels = false
				});
			}

			foreach (var slice in slices)
			{
				chart.Series.Add(slice);
			}

			void Redraw()
			{
				if (chart.ActualWidth <= 1 || chart.ActualHeight <= 1) return;
				chart.Update(true, true);
			}

			if (chart.ActualWidth > 1)
			{
				Redraw();
				return;
			}

			// Page may load before Frame measures — redraw on next idle pass / size change
			Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(Redraw));
			chart.SizeChanged -= PieChart_SizeChangedRedraw;
			chart.SizeChanged += PieChart_SizeChangedRedraw;
		}

		// One-shot SizeChanged redraw when pie chart first gets a real size
		private static void PieChart_SizeChangedRedraw(object sender, SizeChangedEventArgs e)
		{
			if (sender is not PieChart chart) return;
			if (chart.ActualWidth <= 1 || chart.ActualHeight <= 1) return;
			chart.SizeChanged -= PieChart_SizeChangedRedraw;
			chart.Update(true, true);
		}

		// Build pie slices — skip zeros (LiveCharts draws nothing useful for 0-value wedges)
		private static List<PieSeries> BuildPieSlices(List<ChartPoint> points, Color[] colors)
		{
			var culture = CultureInfo.GetCultureInfo("vi-VN");
			var slices = new List<PieSeries>();
			for (int i = 0; i < points.Count; i++)
			{
				double value = points[i].Value;
				if (value <= 0) continue;
				string label = points[i].Label;
				slices.Add(new PieSeries
				{
					Title = label,
					Values = new ChartValues<double> { value },
					Fill = new SolidColorBrush(colors[i % colors.Length]),
					Stroke = Brushes.White,
					StrokeThickness = 1,
					DataLabels = true,
					FontSize = 12,
					LabelPoint = chartPoint =>
						string.Format(culture, "{0}: {1:N0}", label, chartPoint.Y)
				});
			}
			return slices;
		}

		// Map UI filter to (from?, to) for DashboardService; "all" keeps from null
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

		// Compute preset start date; optionally write DatePickers without firing filter events
		private void ApplyRevenuePreset(string key, bool syncPickers)
		{
			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			DateOnly from;
			switch (key)
			{
				case "year":
					from = new DateOnly(today.Year, 1, 1);
					break;
				case "6m":
					from = new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
					break;
				case "month":
					from = new DateOnly(today.Year, today.Month, 1);
					break;
				case "week":
					int offset = ((int)today.DayOfWeek + 6) % 7;
					from = today.AddDays(-offset);
					break;
				case "all":
					from = today.AddYears(-10);
					break;
				default:
					from = dpRevenueFrom.SelectedDate.HasValue
						? DateOnly.FromDateTime(dpRevenueFrom.SelectedDate.Value.Date)
						: today.AddMonths(-1);
					break;
			}

			if (syncPickers)
			{
				_isLoadingFilters = true;
				try
				{
					dpRevenueFrom.SelectedDate = from.ToDateTime(TimeOnly.MinValue);
					dpRevenueTo.SelectedDate = today.ToDateTime(TimeOnly.MinValue);
				}
				finally
				{
					_isLoadingFilters = false;
				}
			}
		}

		private void cbProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingFilters || !IsLoaded) return;
			ReloadDashboard();
		}

		private void cbRevenueRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingFilters || !IsLoaded) return;
			if (cbRevenueRange.SelectedItem is not RevenueRangeOption option) return;
			if (option.Key != "custom")
			{
				ApplyRevenuePreset(option.Key, syncPickers: true);
			}
			ReloadDashboard();
		}

		// Manual date edit switches preset to "custom" then reloads
		private void dpRevenue_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingFilters || !IsLoaded) return;

			_isLoadingFilters = true;
			try
			{
				foreach (RevenueRangeOption item in cbRevenueRange.Items)
				{
					if (item.Key == "custom")
					{
						cbRevenueRange.SelectedItem = item;
						break;
					}
				}
			}
			finally
			{
				_isLoadingFilters = false;
			}

			ReloadDashboard();
		}
	}
}
