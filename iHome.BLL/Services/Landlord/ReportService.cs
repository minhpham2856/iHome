using iHome.BLL.DTOs;
using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.BLL.Services
{
	// Landlord monthly statistics — tables plus daily-revenue and room-status chart series
	public class LandlordReportService
	{
		private readonly IHomeDbContext _db;

		public LandlordReportService() : this(new IHomeDbContext()) { }

		public LandlordReportService(IHomeDbContext db)
		{
			_db = db ?? throw new ArgumentNullException(nameof(db));
		}

		// Property filter — Id=0 means all properties
		public List<PropertyFilterOptionDto> GetPropertyOptions(int landlordId)
		{
			EnsureLandlord(landlordId);
			var options = new List<PropertyFilterOptionDto>
			{
				new() { Id = 0, Name = "Tất cả nhà trọ" }
			};
			options.AddRange(_db.Properties
				.AsNoTracking()
				.Where(p => p.LandlordId == landlordId)
				.OrderBy(p => p.Name)
				.Select(p => new PropertyFilterOptionDto { Id = p.Id, Name = p.Name }));
			return options;
		}

		// Building filter — propertyId > 0 required to load buildings
		public List<BuildingFilterOptionDto> GetBuildingOptions(int landlordId, int propertyId)
		{
			EnsureLandlord(landlordId);
			var options = new List<BuildingFilterOptionDto>
			{
				new() { Id = 0, Name = "Tất cả tòa" }
			};
			if (propertyId <= 0)
			{
				return options;
			}

			EnsureOwnedProperty(landlordId, propertyId);
			options.AddRange(_db.Buildings
				.AsNoTracking()
				.Where(b => b.PropertyId == propertyId && b.Property.LandlordId == landlordId)
				.OrderBy(b => b.Name)
				.Select(b => new BuildingFilterOptionDto
				{
					Id = b.Id,
					PropertyId = b.PropertyId,
					Name = b.Name,
					NumberOfFloors = b.NumberOfFloors
				}));
			return options;
		}

		// Month picker — default last 24 months
		public List<ReportMonthOptionDto> GetMonthOptions(int monthsBack = 24)
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			var options = new List<ReportMonthOptionDto>();
			var cursor = new DateOnly(today.Year, today.Month, 1);
			for (int i = 0; i < monthsBack; i++)
			{
				var month = cursor.AddMonths(-i);
				options.Add(new ReportMonthOptionDto
				{
					Year = month.Year,
					Month = month.Month,
					Name = month.ToString("MM/yyyy", CultureInfo.InvariantCulture)
				});
			}
			return options;
		}

		// Four data tables plus two chart series for the selected month
		public ReportDataDto GetReport(int landlordId, int propertyId, int buildingId, int year, int month)
		{
			EnsureLandlord(landlordId);
			if (propertyId > 0)
			{
				EnsureOwnedProperty(landlordId, propertyId);
			}
			if (buildingId > 0)
			{
				EnsureOwnedBuilding(landlordId, buildingId);
			}

			int? prop = propertyId > 0 ? propertyId : null;
			int? building = buildingId > 0 ? buildingId : null;
			var periodStart = new DateOnly(year, month, 1);
			var periodEnd = periodStart.AddMonths(1).AddDays(-1);
			// InvoiceDate is DateTime — convert DateOnly range to bounds
			var invoiceFrom = periodStart.ToDateTime(TimeOnly.MinValue);
			var invoiceTo = periodEnd.ToDateTime(new TimeOnly(23, 59, 59));

			var rooms = ScopedRooms(landlordId, prop, building)
				.Include(r => r.Building).ThenInclude(b => b.Property)
				.Include(r => r.RoomType)
				.OrderBy(r => r.Building.Property.Name)
				.ThenBy(r => r.Building.Name)
				.ThenBy(r => r.RoomNumber)
				.ToList()
				.Select(r => new ReportRoomRowDto
				{
					PropertyName = r.Building.Property.Name,
					BuildingName = r.Building.Name,
					RoomNumber = r.RoomNumber,
					Floor = r.Floor,
					RoomTypeName = r.RoomType?.TypeName ?? "—",
					StatusDisplay = RoomStatus.Format(r.Status),
					BaseRent = r.RoomType?.BaseRent ?? 0m
				})
				.ToList();

			var revenues = ScopedPayments(landlordId, prop, building)
				.Where(p => p.PaymentDate >= periodStart && p.PaymentDate <= periodEnd)
				.Include(p => p.Invoice).ThenInclude(i => i.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.Building)
				.OrderBy(p => p.PaymentDate)
				.ThenBy(p => p.Invoice.Contract.Room.Building.Name)
				.ThenBy(p => p.Invoice.Contract.Room.RoomNumber)
				.ToList()
				.Select(p => new ReportRevenueRowDto
				{
					PaymentDate = p.PaymentDate,
					PaymentDateDisplay = p.PaymentDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
					BuildingName = p.Invoice.Contract.Room.Building.Name,
					RoomNumber = p.Invoice.Contract.Room.RoomNumber,
					Amount = p.Amount,
					MethodDisplay = PaymentMethod.Format(p.Method)
				})
				.ToList();

			var invoices = ScopedInvoices(landlordId, prop, building)
				.Where(i => i.InvoiceDate >= invoiceFrom && i.InvoiceDate <= invoiceTo)
				.Include(i => i.Contract).ThenInclude(c => c.Room).ThenInclude(r => r.Building)
				.OrderBy(i => i.InvoiceDate)
				.ThenBy(i => i.Contract.Room.Building.Name)
				.ThenBy(i => i.Contract.Room.RoomNumber)
				.ToList()
				.Select(i => new ReportInvoiceRowDto
				{
					InvoiceDate = DateOnly.FromDateTime(i.InvoiceDate),
					InvoiceDateDisplay = i.InvoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
					BuildingName = i.Contract.Room.Building.Name,
					RoomNumber = i.Contract.Room.RoomNumber,
					TotalAmount = i.TotalAmount,
					StatusDisplay = InvoiceStatus.Format(i.Status),
					DueDateDisplay = i.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
				})
				.ToList();

			// Overlap period OR Active contracts that started before period end
			var contracts = ScopedContracts(landlordId, prop, building)
				.Where(c =>
					(c.StartDate <= periodEnd && c.EndDate >= periodStart) ||
					(c.Status == ContractStatus.Active && c.StartDate <= periodEnd))
				.Include(c => c.Room).ThenInclude(r => r.Building)
				.Include(c => c.ContractTenants).ThenInclude(ct => ct.Tenant)
				.OrderBy(c => c.Room.Building.Name)
				.ThenBy(c => c.Room.RoomNumber)
				.ThenBy(c => c.StartDate)
				.ToList()
				.Select(c =>
				{
					var main = c.ContractTenants.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant
						?? c.ContractTenants.Select(ct => ct.Tenant).FirstOrDefault();
					return new ReportContractRowDto
					{
						BuildingName = c.Room.Building.Name,
						RoomNumber = c.Room.RoomNumber,
						MainTenantName = main?.FullName ?? "—",
						StartDateDisplay = c.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
						EndDateDisplay = c.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
						MonthlyRent = c.MonthlyRent,
						StatusDisplay = ContractStatus.FormatShort(c.Status)
					};
				})
				.ToList();

			return new ReportDataDto
			{
				PeriodLabel = periodStart.ToString("MM/yyyy", CultureInfo.InvariantCulture),
				Rooms = rooms,
				Revenues = revenues,
				Invoices = invoices,
				Contracts = contracts,
				RevenueSeries = BuildDailyRevenueSeries(periodStart, periodEnd, revenues),
				RoomStatusSeries = BuildRoomStatusSeries(rooms)
			};
		}

		// Daily revenue chart — one point per calendar day (zero-fill gaps)
		private static List<ChartPoint> BuildDailyRevenueSeries(
			DateOnly periodStart,
			DateOnly periodEnd,
			List<ReportRevenueRowDto> revenues)
		{
			var byDay = revenues
				.GroupBy(r => r.PaymentDate)
				.ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
			var series = new List<ChartPoint>();
			for (var d = periodStart; d <= periodEnd; d = d.AddDays(1))
			{
				byDay.TryGetValue(d, out decimal sum);
				series.Add(new ChartPoint
				{
					Label = d.Day.ToString(CultureInfo.InvariantCulture),
					Value = (double)Math.Round(sum, 0, MidpointRounding.AwayFromZero)
				});
			}
			return series;
		}

		// Room-status pie slices in fixed Occupied/Empty/Deposited/Maintenance order
		private static List<ChartPoint> BuildRoomStatusSeries(List<ReportRoomRowDto> rooms)
		{
			int occupied = rooms.Count(r => r.StatusDisplay == RoomStatus.Occupied);
			int vacant = rooms.Count(r => r.StatusDisplay == RoomStatus.Empty);
			int deposited = rooms.Count(r => r.StatusDisplay == RoomStatus.Deposited);
			int maintenance = rooms.Count(r => r.StatusDisplay == RoomStatus.Maintenance);
			return new List<ChartPoint>
			{
				new() { Label = RoomStatus.Occupied, Value = occupied },
				new() { Label = RoomStatus.Empty, Value = vacant },
				new() { Label = RoomStatus.Deposited, Value = deposited },
				new() { Label = RoomStatus.Maintenance, Value = maintenance }
			};
		}

		private IQueryable<Room> ScopedRooms(int landlordId, int? propertyId, int? buildingId)
		{
			return _db.Rooms.AsNoTracking().Where(r =>
				r.Building.Property.LandlordId == landlordId &&
				(!propertyId.HasValue || r.Building.PropertyId == propertyId.Value) &&
				(!buildingId.HasValue || r.BuildingId == buildingId.Value));
		}

		private IQueryable<Contract> ScopedContracts(int landlordId, int? propertyId, int? buildingId)
		{
			return _db.Contracts.AsNoTracking().Where(c =>
				c.Room.Building.Property.LandlordId == landlordId &&
				(!propertyId.HasValue || c.Room.Building.PropertyId == propertyId.Value) &&
				(!buildingId.HasValue || c.Room.BuildingId == buildingId.Value));
		}

		private IQueryable<Invoice> ScopedInvoices(int landlordId, int? propertyId, int? buildingId)
		{
			return _db.Invoices.AsNoTracking().Where(i =>
				i.Contract.Room.Building.Property.LandlordId == landlordId &&
				(!propertyId.HasValue || i.Contract.Room.Building.PropertyId == propertyId.Value) &&
				(!buildingId.HasValue || i.Contract.Room.BuildingId == buildingId.Value));
		}

		private IQueryable<Payment> ScopedPayments(int landlordId, int? propertyId, int? buildingId)
		{
			return _db.Payments.AsNoTracking().Where(p =>
				p.Invoice.Contract.Room.Building.Property.LandlordId == landlordId &&
				(!propertyId.HasValue || p.Invoice.Contract.Room.Building.PropertyId == propertyId.Value) &&
				(!buildingId.HasValue || p.Invoice.Contract.Room.BuildingId == buildingId.Value));
		}

		private void EnsureLandlord(int landlordId)
		{
			bool ok = _db.Users.Any(u => u.Id == landlordId && u.Role == UserRole.Landlord);
			if (!ok)
			{
				throw new UnauthorizedAccessException("Chỉ chủ trọ mới xem báo cáo thống kê.");
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

		private void EnsureOwnedBuilding(int landlordId, int buildingId)
		{
			bool ok = _db.Buildings.Any(b => b.Id == buildingId && b.Property.LandlordId == landlordId);
			if (!ok)
			{
				throw new UnauthorizedAccessException("Tòa nhà không thuộc về bạn.");
			}
		}
	}
}
