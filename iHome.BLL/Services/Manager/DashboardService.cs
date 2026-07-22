using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Gộp KPI và biểu đồ dashboard Manager theo tòa được phân công
	public class DashboardService
	{
		private readonly ManagerReadRepository _repository = new();

		// Overload tiện — dùng ngày hiện tại làm mốc "hôm nay"
		public DashboardDto GetDashboard(int managerId, int? buildingId = null) =>
			GetDashboard(managerId, buildingId, DateTime.Now);

		// DTO dashboard: đếm, doanh thu, còn thu, ba chuỗi biểu đồ
		public DashboardDto GetDashboard(
			int managerId,
			int? buildingId,
			DateTime referenceDate)
		{
			ServiceGuard.EnsureValidManagerId(managerId);

			var today = DateOnly.FromDateTime(referenceDate);
			var currentMonth = new DateOnly(referenceDate.Year, referenceDate.Month, 1);
			var roomStatusCounts = _repository.GetRoomStatusCounts(managerId, buildingId);
			var contractStatusCounts = _repository.GetContractStatusCounts(managerId, buildingId);
			var monthlyRevenue = _repository.GetMonthlyRevenue(
				managerId,
				currentMonth.AddMonths(-11),
				currentMonth.AddMonths(1),
				buildingId);
			var data = new DashboardDto
			{
				AssignedBuildings = _repository.CountAssignedBuildings(managerId, buildingId),
				TotalRooms = roomStatusCounts.Values.Sum(),
				OccupiedRooms = GetCount(roomStatusCounts, RoomStatus.Occupied),
				VacantRooms = GetCount(roomStatusCounts, RoomStatus.Empty),
				MaintenanceRooms = GetCount(roomStatusCounts, RoomStatus.Maintenance),
				ActiveTenants = _repository.CountActiveTenants(managerId, ContractStatus.Active, buildingId),
				ActiveContracts = GetCount(contractStatusCounts, ContractStatus.Active),
				ExpiringContracts = _repository.CountExpiringContracts(
					managerId,
					ContractStatus.Active,
					today,
					today.AddMonths(1).AddDays(-1),
					buildingId),
				OverdueInvoices = _repository.CountOverdueInvoices(
					managerId,
					InvoiceStatus.Paid,
					today,
					buildingId),
				MonthlyRevenue = GetRevenue(monthlyRevenue, currentMonth),
				OutstandingAmount = _repository.GetOutstandingAmount(
					managerId,
					InvoiceStatus.Paid,
					buildingId)
			};

			BuildRevenueSeries(data, monthlyRevenue, currentMonth);
			BuildRoomStatusSeries(data, roomStatusCounts);
			BuildContractStatusSeries(data, contractStatusCounts);
			return data;
		}

		// Biểu đồ doanh thu 12 tháng (cũ → mới)
		private void BuildRevenueSeries(
			DashboardDto data,
			Dictionary<int, decimal> monthlyRevenue,
			DateOnly currentMonth)
		{
			for (int offset = 11; offset >= 0; offset--)
			{
				var month = currentMonth.AddMonths(-offset);

				data.RevenueSeries.Add(new ChartPointDto
				{
					Label = $"T{month.Month}/{month.Year}",
					Value = (double)GetRevenue(monthlyRevenue, month)
				});
			}
		}

		// Pie trạng thái phòng
		private static void BuildRoomStatusSeries(
			DashboardDto data,
			Dictionary<string, int> roomStatusCounts)
		{
			data.RoomStatusSeries.Add(new ChartPointDto
			{
				Label = RoomStatus.Occupied,
				Value = data.OccupiedRooms
			});
			data.RoomStatusSeries.Add(new ChartPointDto
			{
				Label = RoomStatus.Empty,
				Value = data.VacantRooms
			});
			data.RoomStatusSeries.Add(new ChartPointDto
			{
				Label = RoomStatus.Deposited,
				Value = GetCount(roomStatusCounts, RoomStatus.Deposited)
			});
			data.RoomStatusSeries.Add(new ChartPointDto
			{
				Label = RoomStatus.Maintenance,
				Value = data.MaintenanceRooms
			});
		}

		// Pie trạng thái hợp đồng
		private static void BuildContractStatusSeries(
			DashboardDto data,
			Dictionary<string, int> contractStatusCounts)
		{
			data.ContractStatusSeries.Add(new ChartPointDto
			{
				Label = ContractStatus.Active,
				Value = data.ActiveContracts
			});
			data.ContractStatusSeries.Add(new ChartPointDto
			{
				Label = ContractStatus.Expired,
				Value = GetCount(contractStatusCounts, ContractStatus.Expired)
			});
			data.ContractStatusSeries.Add(new ChartPointDto
			{
				Label = ContractStatus.Terminated,
				Value = GetCount(contractStatusCounts, ContractStatus.Terminated)
			});
		}

		private static int GetCount(Dictionary<string, int> counts, string status) =>
			counts.TryGetValue(status, out var count) ? count : 0;

		// Tra doanh thu theo khóa YYYYMM
		private static decimal GetRevenue(
			Dictionary<int, decimal> revenue,
			DateOnly month)
		{
			var key = month.Year * 100 + month.Month;
			return revenue.TryGetValue(key, out var amount) ? amount : 0m;
		}
	}
}
