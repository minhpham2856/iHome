using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iHome.BLL.DTOs;
using iHome.BLL.Services;
using LiveCharts;
using LiveCharts.Wpf;
using ChartPoint = iHome.BLL.DTOs.ChartPoint;

namespace iHome.UI.Views.Landlord
{
    public partial class DashboardPage : Page
    {
        // placeholder: wire KPI card clicks to drill into detail pages once those pages are built
        // placeholder: add a refresh button that reloads the dashboard data

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // pull every KPI and chart series from the business layer
                var data = new DashboardService().GetDashboardData();
                BindCards(data);
                BindCharts(data);
            }
            catch (Exception)
            {
                // the DB is offline or the connection string is wrong — fail soft instead of crashing
                MessageBox.Show("Không thể tải dữ liệu");
            }
        }

        // copy the KPI values into the named TextBlocks
        private void BindCards(DashboardData data)
        {
            TxtTotalBuildings.Text = data.TotalBuildings.ToString();
            TxtTotalRooms.Text = data.TotalRooms.ToString();
            TxtOccupiedRooms.Text = data.OccupiedRooms.ToString();
            TxtVacantRooms.Text = data.VacantRooms.ToString();
            TxtTotalTenants.Text = data.TotalTenants.ToString();
            TxtActiveContracts.Text = data.ActiveContracts.ToString();
            TxtMonthlyRevenue.Text = data.MonthlyRevenue.ToString("N0");
            TxtOutstanding.Text = data.OutstandingAmount.ToString("N0");
            TxtOverdue.Text = data.OverdueInvoices.ToString();
            TxtExpiring.Text = data.ExpiringContracts.ToString();
        }

        // build the three LiveCharts charts from the series carried in the DTO
        private void BindCharts(DashboardData data)
        {
            // --- revenue column chart: one column per month, labelled by short month name ---
            RevenueChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(data.RevenueSeries.ConvertAll(p => p.Value)),
                    Fill = new SolidColorBrush(Color.FromRgb(44, 62, 80))
                }
            };
            RevenueChart.AxisX = new AxesCollection
            {
                new Axis { Labels = data.RevenueSeries.ConvertAll(p => p.Label).ToArray() }
            };

            // --- room status doughnut ---
            RoomStatusChart.Series = BuildPie(data.RoomStatusSeries,
                Color.FromRgb(46, 204, 113), Color.FromRgb(231, 76, 60), Color.FromRgb(241, 196, 15));

            // --- contract status pie ---
            ContractStatusChart.Series = BuildPie(data.ContractStatusSeries,
                Color.FromRgb(46, 204, 113), Color.FromRgb(231, 76, 60), Color.FromRgb(149, 165, 166));
        }

        // turn a ChartPoint list into one PieSeries per point, cycling the supplied colors
        private SeriesCollection BuildPie(List<ChartPoint> points, params Color[] colors)
        {
            var series = new SeriesCollection();
            for (int i = 0; i < points.Count; i++)
            {
                // each point becomes its own slice; Title shows in the legend, Values holds the single value
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
    }
}
