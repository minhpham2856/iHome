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
	public partial class DashboardPage : Page
	{
		private readonly User _currentUser;
		private readonly int? _buildingId;
		private readonly DashboardService _service = new();
		private bool _isLoading;

		public DashboardPage(User currentUser, int? buildingId)
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
			// Assign local/page state inside DashboardPage without altering business rules
			_currentUser = currentUser;
			// Assign local/page state inside DashboardPage without altering business rules
			_buildingId = buildingId;
			// Subscribe page Loaded event to defer BLL calls until controls exist
			Loaded += DashboardPage_Loaded;
		}

		private void DashboardPage_Loaded(object sender, RoutedEventArgs e) =>
			LoadDashboard();

		private void btnRefresh_Click(object sender, RoutedEventArgs e) =>
			LoadDashboard();

		private void LoadDashboard()
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (_isLoading)
			{
				// Exit method early or return value/tuple to caller
				return;
			}

			// Begin try block so BLL/SQL errors become friendly MessageBox instead of crash
			try
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoading = true;
				// Call helper SetState to refresh UI state from BLL data
				SetState("Đang tải dữ liệu...", true);
				// Enable/disable control during loading or when prerequisites missing
				btnRefresh.IsEnabled = false;
				// ManagerPageAccess.GetManagerId ensures signed-in user is an active manager
				int managerId = ManagerPageAccess.GetManagerId(_currentUser);
				// Call page BLL service GetDashboard to load or mutate scoped data
				var data = _service.GetDashboard(managerId, _buildingId);

				// Call helper BindCards to refresh UI state from BLL data
				BindCards(data);
				// Call helper BindCharts to refresh UI state from BLL data
				BindCharts(data);
				// Update TextBlock/TextBox caption or read user-entered text from control
				lbLastUpdated.Text = $"Cập nhật lúc {DateTime.Now:HH:mm dd/MM/yyyy}";
				// Call helper SetState to refresh UI state from BLL data
				SetState(
					// Assign local/page state inside LoadDashboard without altering business rules
					data.AssignedBuildings == 0
						// Execute UI step inside LoadDashboard
						? "Bạn chưa được phân công quản lý nhà trọ nào."
						// Execute UI step inside LoadDashboard
						: string.Empty,
					// Assign local/page state inside LoadDashboard without altering business rules
					data.AssignedBuildings == 0);
			}
			// Catch expected validation, SMTP, or infrastructure failure for user feedback
			catch (Exception)
			{
				// Call helper SetState to refresh UI state from BLL data
				SetState("Không thể tải dữ liệu bảng điều khiển. Vui lòng thử lại.", true);
			}
			// Always reset loading flags and re-enable refresh regardless of success
			finally
			{
				// Flip internal flag to suppress duplicate events or mark in-flight operation
				_isLoading = false;
				// Enable/disable control during loading or when prerequisites missing
				btnRefresh.IsEnabled = true;
			}
		}

		private void BindCards(DashboardDto data)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbBuildings.Text = data.AssignedBuildings.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbRooms.Text = data.TotalRooms.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOccupied.Text = data.OccupiedRooms.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbVacant.Text = data.VacantRooms.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbMaintenance.Text = data.MaintenanceRooms.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbTenants.Text = data.ActiveTenants.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbContracts.Text = data.ActiveContracts.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbExpiring.Text = data.ExpiringContracts.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOverdue.Text = data.OverdueInvoices.ToString();
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbRevenue.Text = $"{data.MonthlyRevenue:N0} đ";
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbOutstanding.Text = $"{data.OutstandingAmount:N0} đ";
		}

		private void BindCharts(DashboardDto data)
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
					Fill = new SolidColorBrush(GetThemeColor("ColorPrimary"))
				}
			};
			// Configure LiveCharts series/axes from dashboard DTO chart points
			chartRevenue.AxisX = new AxesCollection
			{
				// Configure LiveCharts series/axes from dashboard DTO chart points
				new Axis { Labels = data.RevenueSeries.ConvertAll(p => p.Label).ToArray() }
			};
			// Configure LiveCharts series/axes from dashboard DTO chart points
			chartRevenue.AxisY = new AxesCollection
			{
				// Configure LiveCharts series/axes from dashboard DTO chart points
				new Axis { LabelFormatter = value => value.ToString("N0") }
			};

			// Invoke helper to advance UI workflow without changing logic
			chartRoomStatus.Series = BuildPie(data.RoomStatusSeries,
				// Execute UI step inside BindCharts
				GetThemeColor("ColorInfo"),
				// Execute UI step inside BindCharts
				GetThemeColor("ColorBorder"),
				// Execute UI step inside BindCharts
				GetThemeColor("ColorSuccess"),
				// Execute UI step inside BindCharts
				GetThemeColor("ColorDanger"));
			// Invoke helper to advance UI workflow without changing logic
			chartContractStatus.Series = BuildPie(data.ContractStatusSeries,
				// Execute UI step inside BindCharts
				GetThemeColor("ColorSuccess"),
				// Execute UI step inside BindCharts
				GetThemeColor("ColorDisabled"),
				// Execute UI step inside BindCharts
				GetThemeColor("ColorDanger"));
		}

		private static Color GetThemeColor(string resourceKey)
		{
			// Read themed SolidColorBrush from Styles.xaml
			if (Application.Current.TryFindResource(resourceKey) is SolidColorBrush brush)
			{
				// Exit method early or return value/tuple to caller
				return brush.Color;
			}

			// Throw when role guard or required theme resource is missing
			throw new InvalidOperationException($"Không tìm thấy màu giao diện '{resourceKey}'.");
		}

		private static SeriesCollection BuildPie(
			List<ChartPointDto> points,
			params Color[] colors)
		{
			// Configure LiveCharts series/axes from dashboard DTO chart points
			var series = new SeriesCollection();
			// Iterate collection to update UI, chart series, or CSV rows
			for (int index = 0; index < points.Count; index++)
			{
				// Configure LiveCharts series/axes from dashboard DTO chart points
				series.Add(new PieSeries
				{
					// Assign local/page state inside BuildPie without altering business rules
					Title = points[index].Label,
					// Configure LiveCharts series/axes from dashboard DTO chart points
					Values = new ChartValues<double> { points[index].Value },
					// Assign local/page state inside BuildPie without altering business rules
					Fill = new SolidColorBrush(colors[index % colors.Length]),
					// Assign local/page state inside BuildPie without altering business rules
					DataLabels = true
				// Execute UI step inside BuildPie
				});
			}

			// Exit method early or return value/tuple to caller
			return series;
		}

		private void SetState(string message, bool isVisible)
		{
			// Update TextBlock/TextBox caption or read user-entered text from control
			lbState.Text = message;
			// Show or hide panel/border for empty state or role-specific UI
			brdState.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
