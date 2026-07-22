using iHome.BLL.DTOs.Manager;
using iHome.BLL.Services.Manager;
using iHome.DAL.Entities;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace iHome.UI.Views.Manager
{
	// Trang bảng điều khiển Manager: KPI và biểu đồ tổng quan
	public partial class DashboardPage : Page
	{
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly DashboardService _service = new();
		private bool _isLoading;

		// Khởi tạo theo user và tòa nhà đang chọn
		public DashboardPage(User currentUser, int? buildingId)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_buildingId = buildingId;
			Loaded += DashboardPage_Loaded;
		}

		// Nạp dữ liệu khi trang sẵn sàng
		private void DashboardPage_Loaded(object sender, RoutedEventArgs e) =>
			LoadDashboard();

		// Làm mới bảng điều khiển
		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadDashboard();

		// Tải KPI và biểu đồ từ BLL
		private void LoadDashboard()
		{
			if (_isLoading)
			{
				return;
			}

			try
			{
				_isLoading = true;
				SetState("Đang tải dữ liệu...", true);
				btnRefresh.IsEnabled = false;
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				var data = _service.GetDashboard(managerId, _buildingId);

				BindCards(data);
				BindCharts(data);
				lbLastUpdated.Text = $"Cập nhật lúc {DateTime.Now:HH:mm dd/MM/yyyy}";
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
				btnRefresh.IsEnabled = true;
			}
		}

		// Gán số liệu lên thẻ KPI
		private void BindCards(DashboardDto data)
		{
			lbBuildings.Text = data.AssignedBuildings.ToString();
			lbRooms.Text = data.TotalRooms.ToString();
			lbOccupied.Text = data.OccupiedRooms.ToString();
			lbVacant.Text = data.VacantRooms.ToString();
			lbMaintenance.Text = data.MaintenanceRooms.ToString();
			lbTenants.Text = data.ActiveTenants.ToString();
			lbContracts.Text = data.ActiveContracts.ToString();
			lbExpiring.Text = data.ExpiringContracts.ToString();
			lbOverdue.Text = data.OverdueInvoices.ToString();
			lbRevenue.Text = $"{data.MonthlyRevenue:N0} đ";
			lbOutstanding.Text = $"{data.OutstandingAmount:N0} đ";
		}

		// Vẽ biểu đồ doanh thu và trạng thái phòng/hợp đồng
		private void BindCharts(DashboardDto data)
		{
			chartRevenue.Series = new SeriesCollection
			{
				new ColumnSeries
				{
					Title = "Doanh thu",
					Values = new ChartValues<double>(data.RevenueSeries.ConvertAll(p => p.Value)),
					Fill = new SolidColorBrush(GetThemeColor("ColorPrimary"))
				}
			};
			chartRevenue.AxisX = new AxesCollection
			{
				new Axis { Labels = data.RevenueSeries.ConvertAll(p => p.Label).ToArray() }
			};
			chartRevenue.AxisY = new AxesCollection
			{
				new Axis { LabelFormatter = value => value.ToString("N0") }
			};

			chartRoomStatus.Series = BuildPie(data.RoomStatusSeries,
				GetThemeColor("ColorInfo"),
				GetThemeColor("ColorBorder"),
				GetThemeColor("ColorSuccess"),
				GetThemeColor("ColorDanger"));
			chartContractStatus.Series = BuildPie(data.ContractStatusSeries,
				GetThemeColor("ColorSuccess"),
				GetThemeColor("ColorDisabled"),
				GetThemeColor("ColorDanger"));
		}

		// Đọc màu theme từ Styles.xaml
		private static Color GetThemeColor(string resourceKey)
		{
			if (Application.Current.TryFindResource(resourceKey) is SolidColorBrush brush)
			{
				return brush.Color;
			}

			throw new InvalidOperationException($"Không tìm thấy màu giao diện '{resourceKey}'.");
		}

		// Tạo chuỗi PieSeries; LabelPoint bắt buộc khi DataLabels = true (LiveCharts 0.9.7)
		private static SeriesCollection BuildPie(
			List<ChartPointDto> points,
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
					DataLabels = true,
					LabelPoint = chartPoint => chartPoint.Y.ToString("N0")
				});
			}

			return series;
		}

		// Hiện / ẩn thông báo trạng thái
		private void SetState(string message, bool isVisible)
		{
			lbState.Text = message;
			brdState.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
