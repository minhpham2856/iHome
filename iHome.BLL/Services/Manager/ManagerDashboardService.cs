using iHome.BLL.DTOs;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	public class ManagerDashboardService
	{
		private const string RoomOccupied = "Occupied";
		private const string RoomVacant = "Vacant";
		private const string RoomEmpty = "Empty";
		private const string RoomMaintenance = "Maintenance";
		private const string ContractActive = "Active";
		private const string ContractExpired = "Expired";
		private const string ContractTerminated = "Terminated";
		private const string InvoicePaid = "Paid";
		private readonly ManagerReadRepository _repository = new();

		public ManagerDashboardDto GetDashboard(int managerId, int? buildingId = null) =>
			GetDashboard(managerId, buildingId, DateTime.Now);

		public ManagerDashboardDto GetDashboard(
			int managerId,
			int? buildingId,
			DateTime referenceDate)
		{
			ManagerServiceGuard.EnsureValidManagerId(managerId);

			var today = DateOnly.FromDateTime(referenceDate);
			var currentMonth = new DateOnly(referenceDate.Year, referenceDate.Month, 1);
			var roomStatusCounts = _repository.GetRoomStatusCounts(managerId, buildingId);
			var contractStatusCounts = _repository.GetContractStatusCounts(managerId, buildingId);
			var monthlyRevenue = _repository.GetMonthlyRevenue(
				managerId,
				currentMonth.AddMonths(-11),
				currentMonth.AddMonths(1),
				buildingId);
			var data = new ManagerDashboardDto
			{
				AssignedBuildings = _repository.CountAssignedBuildings(managerId, buildingId),
				TotalRooms = roomStatusCounts.Values.Sum(),
				OccupiedRooms = GetCount(roomStatusCounts, RoomOccupied),
				VacantRooms = GetCount(roomStatusCounts, RoomVacant) +
					GetCount(roomStatusCounts, RoomEmpty),
				MaintenanceRooms = GetCount(roomStatusCounts, RoomMaintenance),
				ActiveTenants = _repository.CountActiveTenants(managerId, ContractActive, buildingId),
				ActiveContracts = GetCount(contractStatusCounts, ContractActive),
				ExpiringContracts = _repository.CountExpiringContracts(
					managerId,
					ContractActive,
					today,
					today.AddMonths(1).AddDays(-1),
					buildingId),
				OverdueInvoices = _repository.CountOverdueInvoices(
					managerId,
					InvoicePaid,
					today,
					buildingId),
				MonthlyRevenue = GetRevenue(monthlyRevenue, currentMonth),
				OutstandingAmount = _repository.GetOutstandingAmount(
					managerId,
					InvoicePaid,
					buildingId)
			};

			BuildRevenueSeries(data, monthlyRevenue, currentMonth);
			BuildRoomStatusSeries(data);
			BuildContractStatusSeries(data, contractStatusCounts);

			return data;
		}

		private void BuildRevenueSeries(
			ManagerDashboardDto data,
			Dictionary<int, decimal> monthlyRevenue,
			DateOnly currentMonth)
		{
			for (int offset = 11; offset >= 0; offset--)
			{
				var month = currentMonth.AddMonths(-offset);

				data.RevenueSeries.Add(new ManagerChartPointDto
				{
					Label = $"T{month.Month}/{month.Year}",
					Value = (double)GetRevenue(monthlyRevenue, month)
				});
			}
		}

		private static void BuildRoomStatusSeries(ManagerDashboardDto data)
		{
			data.RoomStatusSeries.Add(new ManagerChartPointDto
			{
				Label = "Đang ở",
				Value = data.OccupiedRooms
			});
			data.RoomStatusSeries.Add(new ManagerChartPointDto
			{
				Label = "Còn trống",
				Value = data.VacantRooms
			});
			data.RoomStatusSeries.Add(new ManagerChartPointDto
			{
				Label = "Bảo trì",
				Value = data.MaintenanceRooms
			});
		}

		private static void BuildContractStatusSeries(
			ManagerDashboardDto data,
			Dictionary<string, int> contractStatusCounts)
		{
			data.ContractStatusSeries.Add(new ManagerChartPointDto
			{
				Label = "Đang hoạt động",
				Value = data.ActiveContracts
			});
			data.ContractStatusSeries.Add(new ManagerChartPointDto
			{
				Label = "Đã hết hạn",
				Value = GetCount(contractStatusCounts, ContractExpired)
			});
			data.ContractStatusSeries.Add(new ManagerChartPointDto
			{
				Label = "Đã chấm dứt",
				Value = GetCount(contractStatusCounts, ContractTerminated)
			});
		}

		private static int GetCount(Dictionary<string, int> counts, string status) =>
			counts.TryGetValue(status, out var count) ? count : 0;

		private static decimal GetRevenue(
			Dictionary<int, decimal> revenue,
			DateOnly month)
		{
			var key = month.Year * 100 + month.Month;
			return revenue.TryGetValue(key, out var amount) ? amount : 0m;
		}
	}
}
