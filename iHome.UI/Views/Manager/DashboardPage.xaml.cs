using iHome.BLL.DTOs;
using iHome.BLL.Services.Manager;
using iHome.DAL.Entities;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace iHome.UI.Views.Manager
{
	public partial class DashboardPage : Page
	{
		private readonly User _currentUser;
		private readonly int? _propertyId;
		private readonly ManagerDashboardService _service = new();
		private bool _isLoading;

		public DashboardPage(User currentUser, int? propertyId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_propertyId = propertyId;
			Loaded += DashboardPage_Loaded;
		}

		private async void DashboardPage_Loaded(object sender, RoutedEventArgs e) =>
			await LoadDashboardAsync();

		private async void BtnRefresh_Click(object sender, RoutedEventArgs e) =>
			await LoadDashboardAsync();

		private async Task LoadDashboardAsync()
		{
			if (_isLoading)
			{
				return;
			}

			try
			{
				_isLoading = true;
				SetState("Đang tải dữ liệu...", true);
				BtnRefresh.IsEnabled = false;
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				var data = await Task.Run(() => _service.GetDashboard(managerId, _propertyId));

				BindCards(data);
				BindCharts(data);
				TxtLastUpdated.Text = $"Cập nhật lúc {DateTime.Now:HH:mm dd/MM/yyyy}";
				SetState(
					data.AssignedBuildings == 0
						? "Bạn chưa được phân công quản lý nhà trọ nào."
						: string.Empty,
					data.AssignedBuildings == 0);
			}
			catch (Exception)
			{
				SetState("Không thể tải dữ liệu bảng điều khiển. Vui lòng thử lại.", true);
			}
			finally
			{
				_isLoading = false;
				BtnRefresh.IsEnabled = true;
			}
		}

		private void BindCards(ManagerDashboardDto data)
		{
			TxtBuildings.Text = data.AssignedBuildings.ToString();
			TxtRooms.Text = data.TotalRooms.ToString();
			TxtOccupied.Text = data.OccupiedRooms.ToString();
			TxtVacant.Text = data.VacantRooms.ToString();
			TxtMaintenance.Text = data.MaintenanceRooms.ToString();
			TxtTenants.Text = data.ActiveTenants.ToString();
			TxtContracts.Text = data.ActiveContracts.ToString();
			TxtExpiring.Text = data.ExpiringContracts.ToString();
			TxtOverdue.Text = data.OverdueInvoices.ToString();
			TxtRevenue.Text = $"{data.MonthlyRevenue:N0} đ";
			TxtOutstanding.Text = $"{data.OutstandingAmount:N0} đ";
		}

		private void BindCharts(ManagerDashboardDto data)
		{
			RevenueChart.Series = new SeriesCollection
			{
				new ColumnSeries
				{
					Title = "Doanh thu",
					Values = new ChartValues<double>(data.RevenueSeries.ConvertAll(p => p.Value)),
					Fill = new SolidColorBrush(GetThemeColor("ColorPrimary"))
				}
			};
			RevenueChart.AxisX = new AxesCollection
			{
				new Axis { Labels = data.RevenueSeries.ConvertAll(p => p.Label).ToArray() }
			};
			RevenueChart.AxisY = new AxesCollection
			{
				new Axis { LabelFormatter = value => value.ToString("N0") }
			};

			RoomStatusChart.Series = BuildPie(data.RoomStatusSeries,
				GetThemeColor("ColorInfo"),
				GetThemeColor("ColorBorder"),
				GetThemeColor("ColorDanger"));
			ContractStatusChart.Series = BuildPie(data.ContractStatusSeries,
				GetThemeColor("ColorSuccess"),
				GetThemeColor("ColorDisabled"),
				GetThemeColor("ColorDanger"));
		}

		private static Color GetThemeColor(string resourceKey)
		{
			if (Application.Current.TryFindResource(resourceKey) is SolidColorBrush brush)
			{
				return brush.Color;
			}

			throw new InvalidOperationException($"Không tìm thấy màu giao diện '{resourceKey}'.");
		}

		private static SeriesCollection BuildPie(
			List<ManagerChartPointDto> points,
			params Color[] colors)
		{
			var series = new SeriesCollection();
			for (int index = 0; index < points.Count; index++)
			{
				series.Add(new PieSeries
				{
					Title = points[index].Label,
					Values = new ChartValues<double> { points[index].Value },
					Fill = new SolidColorBrush(colors[index % colors.Length]),
					DataLabels = true
				});
			}

			return series;
		}

		private void SetState(string message, bool isVisible)
		{
			StateText.Text = message;
			StatePanel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
