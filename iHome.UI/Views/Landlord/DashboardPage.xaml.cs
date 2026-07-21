using System;
using System.Collections.Generic;
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
	public partial class DashboardPage : Page
	{
		private readonly User _currentUser;
		private readonly DashboardService _service = new();
		private bool _isLoadingFilters;

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

		private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= DashboardPage_Loaded;
			InitFilters();
			ReloadDashboard();
		}

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

		private int? SelectedPropertyId =>
			(cbProperty.SelectedItem as DashboardPropertyOption)?.Id;

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
				lblRevenueTitle.Text = $"Doanh thu ({rangeText})";
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Không thể tải dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void BindCards(DashboardData data)
		{
			lblTotalProperties.Text = data.TotalProperties.ToString();
			lblTotalBuildings.Text = data.TotalBuildings.ToString();
			lblTotalRooms.Text = data.TotalRooms.ToString();
			lblOccupiedRooms.Text = data.OccupiedRooms.ToString();
			lblVacantRooms.Text = data.VacantRooms.ToString();
			lblTotalTenants.Text = data.TotalTenants.ToString();
			lblActiveContracts.Text = data.ActiveContracts.ToString();
			lblMonthlyRevenue.Text = data.MonthlyRevenue.ToString("N0");
			lblOutstanding.Text = data.OutstandingAmount.ToString("N0");
			lblOverdue.Text = data.OverdueInvoices.ToString();
		}

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
				new Axis { MinValue = 0 }
			};

			chartRoomStatus.Series = BuildPie(data.RoomStatusSeries,
				Color.FromRgb(46, 204, 113), Color.FromRgb(231, 76, 60), Color.FromRgb(241, 196, 15));

			chartContractStatus.Series = BuildPie(data.ContractStatusSeries,
				Color.FromRgb(46, 204, 113), Color.FromRgb(231, 76, 60), Color.FromRgb(149, 165, 166));
		}

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
					DataLabels = true
				});
			}
			return series;
		}

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

		private void dpRevenue_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoadingFilters || !IsLoaded) return;

			// người dùng chỉnh tay → chuyển sang Tùy chọn
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
