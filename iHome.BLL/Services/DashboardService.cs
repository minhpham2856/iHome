using iHome.BLL.DTOs;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;

namespace iHome.BLL.Services
{
	public class DashboardService
	{
		// repositories are created with new — no dependency injection in this project
		private readonly BuildingRepository _buildings = new();
		private readonly RoomRepository _rooms = new();
		private readonly TenantRepository _tenants = new();
		private readonly ContractRepository _contracts = new();
		private readonly InvoiceRepository _invoices = new();
		private readonly PaymentRepository _payments = new();

		// status literals are centralized here so they are easy to correct if the DB uses different values
		private const string RoomOccupied = "Occupied";
		private const string RoomVacant = "Vacant";
		private const string RoomMaintenance = "Maintenance";
		private const string ContractActive = "Active";
		private const string ContractExpired = "Expired";
		private const string ContractTerminated = "Terminated";
		private const string InvoicePaid = "Paid";

		// gather every KPI and chart series the dashboard needs
		public DashboardData GetDashboardData()
		{
			var data = new DashboardData();

			// --- KPI cards ---
			data.TotalBuildings = _buildings.GetAll().Count;
			data.TotalRooms = _rooms.GetAll().Count;
			data.OccupiedRooms = _rooms.CountByStatus(RoomOccupied);
			data.VacantRooms = _rooms.CountByStatus(RoomVacant);
			data.TotalTenants = _tenants.Count();
			data.ActiveContracts = _contracts.CountByStatus(ContractActive);
			data.OutstandingAmount = _invoices.OutstandingTotal(InvoicePaid);
			data.OverdueInvoices = _invoices.OverdueCount(InvoicePaid);
			data.ExpiringContracts = _contracts.ExpiringSoon(ContractActive, 30).Count;

			// this month's revenue comes from payments recorded in the current month/year
			var now = DateTime.Now;
			data.MonthlyRevenue = _payments.SumForMonth(now.Year, now.Month);

			// --- revenue over the last 12 months (column chart) ---
			// walk backwards from the current month so the series reads left-to-right, oldest first
			for (int i = 11; i >= 0; i--)
			{
				var month = now.AddMonths(-i);
				var amount = _payments.SumForMonth(month.Year, month.Month);
				data.RevenueSeries.Add(new ChartPoint
				{
					Label = month.ToString("MMM"),
					Value = (double)amount
				});
			}

			// --- room status breakdown (doughnut) ---
			data.RoomStatusSeries.Add(new ChartPoint { Label = "Occupied", Value = data.OccupiedRooms });
			data.RoomStatusSeries.Add(new ChartPoint { Label = "Vacant", Value = data.VacantRooms });
			data.RoomStatusSeries.Add(new ChartPoint { Label = "Maintenance", Value = _rooms.CountByStatus(RoomMaintenance) });

			// --- contract status breakdown (pie) ---
			data.ContractStatusSeries.Add(new ChartPoint { Label = "Active", Value = data.ActiveContracts });
			data.ContractStatusSeries.Add(new ChartPoint { Label = "Expired", Value = _contracts.CountByStatus(ContractExpired) });
			data.ContractStatusSeries.Add(new ChartPoint { Label = "Terminated", Value = _contracts.CountByStatus(ContractTerminated) });

			return data;
		}
	}
}
