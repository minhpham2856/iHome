using iHome.BLL.DTOs.Manager;
using iHome.BLL.Enums;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.BLL.Services.Manager
{
	// Aggregate KPI and chart data for Manager dashboard — scoped to assigned buildings
	public class DashboardService
	{
		// Read repository for all manager-scoped aggregate queries
		private readonly ManagerReadRepository _repository = new();

		// Convenience overload — use current date as reference for "today" calculations
		public DashboardDto GetDashboard(int managerId, int? buildingId = null) =>
			GetDashboard(managerId, buildingId, DateTime.Now);

		// Build full dashboard DTO: counts, revenue, outstanding, three chart series
		public DashboardDto GetDashboard(
			int managerId,
			int? buildingId,
			DateTime referenceDate)
		{
			ServiceGuard.EnsureValidManagerId(managerId);

			// Calendar "today" from reference date — used for overdue/expiring thresholds
			var today = DateOnly.FromDateTime(referenceDate);
			// First day of reference month — anchor for 12-month revenue window
			var currentMonth = new DateOnly(referenceDate.Year, referenceDate.Month, 1);
			// Room counts grouped by status string
			var roomStatusCounts = _repository.GetRoomStatusCounts(managerId, buildingId);
			// Contract counts grouped by status string
			var contractStatusCounts = _repository.GetContractStatusCounts(managerId, buildingId);
			// Revenue keyed by YYYYMM int for last 12 months
			var monthlyRevenue = _repository.GetMonthlyRevenue(
				managerId,
				currentMonth.AddMonths(-11),
				currentMonth.AddMonths(1),
				buildingId);
			// Populate scalar KPI fields on dashboard DTO
			var data = new DashboardDto
			{
				AssignedBuildings = _repository.CountAssignedBuildings(managerId, buildingId),
				TotalRooms = roomStatusCounts.Values.Sum(),
				OccupiedRooms = GetCount(roomStatusCounts, RoomStatus.Occupied),
				VacantRooms = GetCount(roomStatusCounts, RoomStatus.Empty),
				MaintenanceRooms = GetCount(roomStatusCounts, RoomStatus.Maintenance),
				ActiveTenants = _repository.CountActiveTenants(managerId, ContractStatus.Active, buildingId),
				ActiveContracts = GetCount(contractStatusCounts, ContractStatus.Active),
				// Contracts ending within next calendar month
				ExpiringContracts = _repository.CountExpiringContracts(
					managerId,
					ContractStatus.Active,
					today,
					today.AddMonths(1).AddDays(-1),
					buildingId),
				// Unpaid invoices past due date
				OverdueInvoices = _repository.CountOverdueInvoices(
					managerId,
					InvoiceStatus.Paid,
					today,
					buildingId),
				// Sum payments in current calendar month
				MonthlyRevenue = GetRevenue(monthlyRevenue, currentMonth),
				// Total unpaid balance across non-Paid invoices
				OutstandingAmount = _repository.GetOutstandingAmount(
					managerId,
					InvoiceStatus.Paid,
					buildingId)
			};

			// Append chart series collections to DTO
			BuildRevenueSeries(data, monthlyRevenue, currentMonth);
			BuildRoomStatusSeries(data, roomStatusCounts);
			BuildContractStatusSeries(data, contractStatusCounts);
			return data;
		}

		// Fill 12-point monthly revenue line chart (oldest → newest)
		private void BuildRevenueSeries(
			DashboardDto data,
			Dictionary<int, decimal> monthlyRevenue,
			DateOnly currentMonth)
		{
			// Walk back 11 months through current month inclusive
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

		// Pie chart slices for room status distribution
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

		// Pie chart slices for contract status distribution
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

		// Safe lookup in status count dictionary — missing key → 0
		private static int GetCount(Dictionary<string, int> counts, string status) =>
			counts.TryGetValue(status, out var count) ? count : 0;

		// Lookup revenue for a month using YYYYMM composite key
		private static decimal GetRevenue(
			Dictionary<int, decimal> revenue,
			DateOnly month)
		{
			var key = month.Year * 100 + month.Month;
			return revenue.TryGetValue(key, out var amount) ? amount : 0m;
		}
	}
}
