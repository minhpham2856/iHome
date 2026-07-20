using iHome.BLL.DTOs;
using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.BLL.Services
{
	public class DashboardService
	{
		private readonly IHomeDbContext _db;

		private const string RoomOccupied = "Occupied";
		private const string RoomVacant = "Empty";
		private const string RoomVacantAlt = "Vacant";
		private const string RoomMaintenance = "Maintenance";
		private const string ContractActive = "Active";
		private const string ContractExpired = "Expired";
		private const string ContractTerminated = "Terminated";
		private const string InvoicePaid = "Paid";

		public DashboardService() : this(new IHomeDbContext())
		{
		}

		public DashboardService(IHomeDbContext db)
		{
			_db = db ?? throw new ArgumentNullException(nameof(db));
		}

		public List<DashboardPropertyOption> GetPropertyOptions(int landlordId)
		{
			var options = new List<DashboardPropertyOption>
			{
				new() { Id = null, Name = "Tất cả nhà trọ" }
			};
			options.AddRange(_db.Properties
				.AsNoTracking()
				.Where(p => p.LandlordId == landlordId)
				.OrderBy(p => p.Name)
				.Select(p => new DashboardPropertyOption { Id = p.Id, Name = p.Name }));
			return options;
		}

		public DashboardData GetDashboardData(
			int landlordId,
			int? propertyId = null,
			DateOnly? revenueFrom = null,
			DateOnly? revenueTo = null)
		{
			EnsureLandlord(landlordId);
			if (propertyId.HasValue)
			{
				EnsureOwnedProperty(landlordId, propertyId.Value);
			}

			var data = new DashboardData();
			var now = DateTime.Now;
			var today = DateOnly.FromDateTime(now);

			var properties = _db.Properties
				.AsNoTracking()
				.Where(p => p.LandlordId == landlordId && (!propertyId.HasValue || p.Id == propertyId.Value));

			data.TotalProperties = properties.Count();

			var buildings = _db.Buildings
				.AsNoTracking()
				.Where(b => b.Property.LandlordId == landlordId &&
					(!propertyId.HasValue || b.PropertyId == propertyId.Value));
			data.TotalBuildings = buildings.Count();

			var rooms = _db.Rooms
				.AsNoTracking()
				.Where(r => r.Building.Property.LandlordId == landlordId &&
					(!propertyId.HasValue || r.Building.PropertyId == propertyId.Value));
			data.TotalRooms = rooms.Count();
			data.OccupiedRooms = rooms.Count(r => r.Status == RoomOccupied);
			data.VacantRooms = rooms.Count(r => r.Status == RoomVacant || r.Status == RoomVacantAlt);
			int maintenance = rooms.Count(r => r.Status == RoomMaintenance);

			var contracts = _db.Contracts
				.AsNoTracking()
				.Where(c => c.Room.Building.Property.LandlordId == landlordId &&
					(!propertyId.HasValue || c.Room.Building.PropertyId == propertyId.Value));
			data.ActiveContracts = contracts.Count(c => c.Status == ContractActive);
			int expired = contracts.Count(c => c.Status == ContractExpired);
			int terminated = contracts.Count(c => c.Status == ContractTerminated);

			data.TotalTenants = _db.ContractTenants
				.AsNoTracking()
				.Where(ct => ct.Contract.Room.Building.Property.LandlordId == landlordId &&
					(!propertyId.HasValue || ct.Contract.Room.Building.PropertyId == propertyId.Value) &&
					ct.Contract.Status == ContractActive)
				.Select(ct => ct.TenantId)
				.Distinct()
				.Count();

			var invoices = _db.Invoices
				.AsNoTracking()
				.Where(i => i.Contract.Room.Building.Property.LandlordId == landlordId &&
					(!propertyId.HasValue || i.Contract.Room.Building.PropertyId == propertyId.Value));
			data.OutstandingAmount = invoices
				.Where(i => i.Status != InvoicePaid)
				.Select(i => (decimal?)i.TotalAmount)
				.Sum() ?? 0m;
			data.OverdueInvoices = invoices.Count(i => i.Status != InvoicePaid && i.DueDate < today);

			var payments = ScopedPayments(landlordId, propertyId);
			data.MonthlyRevenue = payments
				.Where(p => p.PaymentDate.Year == now.Year && p.PaymentDate.Month == now.Month)
				.Select(p => (decimal?)p.Amount)
				.Sum() ?? 0m;

			DateOnly from = revenueFrom ?? ResolveDefaultFrom(payments, today);
			DateOnly to = revenueTo ?? today;
			if (to < from)
			{
				(from, to) = (to, from);
			}
			data.RevenueSeries = BuildRevenueSeries(payments, from, to);

			data.RoomStatusSeries.Add(new ChartPoint { Label = "Đang ở", Value = data.OccupiedRooms });
			data.RoomStatusSeries.Add(new ChartPoint { Label = "Trống", Value = data.VacantRooms });
			data.RoomStatusSeries.Add(new ChartPoint { Label = "Bảo trì", Value = maintenance });

			data.ContractStatusSeries.Add(new ChartPoint { Label = "Hoạt động", Value = data.ActiveContracts });
			data.ContractStatusSeries.Add(new ChartPoint { Label = "Hết hạn", Value = expired });
			data.ContractStatusSeries.Add(new ChartPoint { Label = "Đã hủy", Value = terminated });

			return data;
		}

		private IQueryable<Payment> ScopedPayments(int landlordId, int? propertyId) =>
			_db.Payments
				.AsNoTracking()
				.Where(p => p.Invoice.Contract.Room.Building.Property.LandlordId == landlordId &&
					(!propertyId.HasValue || p.Invoice.Contract.Room.Building.PropertyId == propertyId.Value));

		private static DateOnly ResolveDefaultFrom(IQueryable<Payment> payments, DateOnly today)
		{
			var min = payments.Select(p => (DateOnly?)p.PaymentDate).Min();
			return min ?? today.AddMonths(-11).AddDays(1 - today.Day);
		}

		private static List<ChartPoint> BuildRevenueSeries(IQueryable<Payment> payments, DateOnly from, DateOnly to)
		{
			var rows = payments
				.Where(p => p.PaymentDate >= from && p.PaymentDate <= to)
				.Select(p => new { p.PaymentDate, p.Amount })
				.ToList();

			int daySpan = to.DayNumber - from.DayNumber + 1;
			var series = new List<ChartPoint>();

			if (daySpan <= 45)
			{
				for (var d = from; d <= to; d = d.AddDays(1))
				{
					decimal sum = rows.Where(r => r.PaymentDate == d).Sum(r => r.Amount);
					series.Add(new ChartPoint
					{
						Label = d.ToString("dd/MM", CultureInfo.InvariantCulture),
						Value = (double)sum
					});
				}
				return series;
			}

			var cursor = new DateOnly(from.Year, from.Month, 1);
			var endMonth = new DateOnly(to.Year, to.Month, 1);
			while (cursor <= endMonth)
			{
				decimal sum = rows
					.Where(r => r.PaymentDate.Year == cursor.Year && r.PaymentDate.Month == cursor.Month)
					.Sum(r => r.Amount);
				series.Add(new ChartPoint
				{
					Label = cursor.ToString("MM/yyyy", CultureInfo.InvariantCulture),
					Value = (double)sum
				});
				cursor = cursor.AddMonths(1);
			}
			return series;
		}

		private void EnsureLandlord(int landlordId)
		{
			bool ok = _db.Users.Any(u => u.Id == landlordId && u.Role == "Landlord");
			if (!ok)
			{
				throw new UnauthorizedAccessException("Chỉ chủ trọ mới xem bảng điều khiển này.");
			}
		}

		private void EnsureOwnedProperty(int landlordId, int propertyId)
		{
			bool ok = _db.Properties.Any(p => p.Id == propertyId && p.LandlordId == landlordId);
			if (!ok)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
		}
	}
}
