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
	// Landlord monthly statistics report — rooms, revenue, invoices, contracts plus chart series
	public class LandlordReportService
	{
		// Shared EF Core context instance
		private readonly IHomeDbContext _db;

		public LandlordReportService() : this(new IHomeDbContext()) { }

		public LandlordReportService(IHomeDbContext db)
		{
			// Reject null DbContext dependency
			_db = db ?? throw new ArgumentNullException(nameof(db));
		}

		// Property filter combo — Id=0 means all properties
		public List<PropertyFilterOptionDto> GetPropertyOptions(int landlordId)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Start with "all properties" placeholder
			var options = new List<PropertyFilterOptionDto>
			{
				new() { Id = 0, Name = "Tất cả nhà trọ" }
			};
			// Append landlord properties sorted by name
			options.AddRange(_db.Properties
				.AsNoTracking()
				.Where(p => p.LandlordId == landlordId)
				.OrderBy(p => p.Name)
				.Select(p => new PropertyFilterOptionDto { Id = p.Id, Name = p.Name }));
			// Return filter combo options
			return options;
		}

		// Building filter combo — requires propertyId > 0 to load buildings
		public List<BuildingFilterOptionDto> GetBuildingOptions(int landlordId, int propertyId)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Start with "all buildings" placeholder
			var options = new List<BuildingFilterOptionDto>
			{
				new() { Id = 0, Name = "Tất cả tòa" }
			};
			// No specific property selected — return placeholder only
			if (propertyId <= 0)
			{
				return options;
			}

			// Verify property belongs to landlord
			EnsureOwnedProperty(landlordId, propertyId);
			// Append buildings for property sorted by name
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
			// Return filter combo options
			return options;
		}

		// Report month combo — default last 24 months
		public List<ReportMonthOptionDto> GetMonthOptions(int monthsBack = 24)
		{
			// Anchor cursor at first day of current month
			var today = DateOnly.FromDateTime(DateTime.Today);
			// Accumulate month options walking backward
			var options = new List<ReportMonthOptionDto>();
			var cursor = new DateOnly(today.Year, today.Month, 1);
			// Emit one option per month in range
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
			// Return month picker options
			return options;
		}

		// Aggregate monthly report: four data tables plus two chart series
		public ReportDataDto GetReport(int landlordId, int propertyId, int buildingId, int year, int month)
		{
			// Verify caller is a landlord
			EnsureLandlord(landlordId);
			// Verify property ownership when filtering by property
			if (propertyId > 0)
			{
				EnsureOwnedProperty(landlordId, propertyId);
			}
			// Verify building ownership when filtering by building
			if (buildingId > 0)
			{
				EnsureOwnedBuilding(landlordId, buildingId);
			}

			// Id 0 means all — convert to null for scoped queries
			int? prop = propertyId > 0 ? propertyId : null;
			int? building = buildingId > 0 ? buildingId : null;
			// Compute inclusive DateOnly range for selected month
			var periodStart = new DateOnly(year, month, 1);
			var periodEnd = periodStart.AddMonths(1).AddDays(-1);
			// InvoiceDate is DateTime — convert DateOnly range to DateTime bounds
			var invoiceFrom = periodStart.ToDateTime(TimeOnly.MinValue);
			var invoiceTo = periodEnd.ToDateTime(new TimeOnly(23, 59, 59));

			// Load scoped rooms with navigation and map to report rows
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

			// Load payments in period with invoice/room navigation and map to revenue rows
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

			// Load invoices in period with contract/room navigation and map to invoice rows
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

			// Contracts overlapping report period OR Active contracts started before period end
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
					// Prefer main tenant; fall back to first linked tenant
					var main = c.ContractTenants.FirstOrDefault(ct => ct.IsMainTenant)?.Tenant
						?? c.ContractTenants.Select(ct => ct.Tenant).FirstOrDefault();
					// Build contract report row
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

			// Assemble full report payload with tables and chart series
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

		// Daily revenue chart for the month — label = day number (1..31)
		private static List<ChartPoint> BuildDailyRevenueSeries(
			DateOnly periodStart,
			DateOnly periodEnd,
			List<ReportRevenueRowDto> revenues)
		{
			// Group revenue rows by payment date
			var byDay = revenues
				.GroupBy(r => r.PaymentDate)
				.ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
			// Emit one chart point per calendar day in period
			var series = new List<ChartPoint>();
			for (var d = periodStart; d <= periodEnd; d = d.AddDays(1))
			{
				// Default to zero when no payments on this day
				byDay.TryGetValue(d, out decimal sum);
				series.Add(new ChartPoint
				{
					Label = d.Day.ToString(CultureInfo.InvariantCulture),
					Value = (double)Math.Round(sum, 0, MidpointRounding.AwayFromZero)
				});
			}
			// Return daily revenue series
			return series;
		}

		// Room status pie chart from formatted StatusDisplay values
		private static List<ChartPoint> BuildRoomStatusSeries(List<ReportRoomRowDto> rooms)
		{
			// Count rooms by formatted status label
			int occupied = rooms.Count(r => r.StatusDisplay == RoomStatus.Occupied);
			int vacant = rooms.Count(r => r.StatusDisplay == RoomStatus.Empty);
			int deposited = rooms.Count(r => r.StatusDisplay == RoomStatus.Deposited);
			int maintenance = rooms.Count(r => r.StatusDisplay == RoomStatus.Maintenance);
			// Return fixed-order status slices
			return new List<ChartPoint>
			{
				new() { Label = RoomStatus.Occupied, Value = occupied },
				new() { Label = RoomStatus.Empty, Value = vacant },
				new() { Label = RoomStatus.Deposited, Value = deposited },
				new() { Label = RoomStatus.Maintenance, Value = maintenance }
			};
		}

		// IQueryable rooms scoped by landlord and optional property/building filters
		private IQueryable<Room> ScopedRooms(int landlordId, int? propertyId, int? buildingId)
		{
			// Filter rooms through building.property landlord chain
			return _db.Rooms.AsNoTracking().Where(r =>
				r.Building.Property.LandlordId == landlordId &&
				(!propertyId.HasValue || r.Building.PropertyId == propertyId.Value) &&
				(!buildingId.HasValue || r.BuildingId == buildingId.Value));
		}

		// IQueryable contracts scoped by landlord and optional property/building filters
		private IQueryable<Contract> ScopedContracts(int landlordId, int? propertyId, int? buildingId)
		{
			// Filter contracts through room.building.property landlord chain
			return _db.Contracts.AsNoTracking().Where(c =>
				c.Room.Building.Property.LandlordId == landlordId &&
				(!propertyId.HasValue || c.Room.Building.PropertyId == propertyId.Value) &&
				(!buildingId.HasValue || c.Room.BuildingId == buildingId.Value));
		}

		// IQueryable invoices scoped by landlord and optional property/building filters
		private IQueryable<Invoice> ScopedInvoices(int landlordId, int? propertyId, int? buildingId)
		{
			// Filter invoices through contract.room.building.property landlord chain
			return _db.Invoices.AsNoTracking().Where(i =>
				i.Contract.Room.Building.Property.LandlordId == landlordId &&
				(!propertyId.HasValue || i.Contract.Room.Building.PropertyId == propertyId.Value) &&
				(!buildingId.HasValue || i.Contract.Room.BuildingId == buildingId.Value));
		}

		// IQueryable payments scoped by landlord and optional property/building filters
		private IQueryable<Payment> ScopedPayments(int landlordId, int? propertyId, int? buildingId)
		{
			// Filter payments through invoice.contract.room.building.property landlord chain
			return _db.Payments.AsNoTracking().Where(p =>
				p.Invoice.Contract.Room.Building.Property.LandlordId == landlordId &&
				(!propertyId.HasValue || p.Invoice.Contract.Room.Building.PropertyId == propertyId.Value) &&
				(!buildingId.HasValue || p.Invoice.Contract.Room.BuildingId == buildingId.Value));
		}

		// Guard caller has Landlord role
		private void EnsureLandlord(int landlordId)
		{
			// Check Users table for landlord role
			bool ok = _db.Users.Any(u => u.Id == landlordId && u.Role == UserRole.Landlord);
			// Reject non-landlord callers
			if (!ok)
			{
				throw new UnauthorizedAccessException("Chỉ chủ trọ mới xem báo cáo thống kê.");
			}
		}

		// Guard property owned by landlord when filtering by property
		private void EnsureOwnedProperty(int landlordId, int propertyId)
		{
			// Check Properties table for landlord ownership
			bool ok = _db.Properties.Any(p => p.Id == propertyId && p.LandlordId == landlordId);
			// Reject foreign-owned property filter
			if (!ok)
			{
				throw new UnauthorizedAccessException("Nhà trọ không thuộc về bạn.");
			}
		}

		// Guard building owned by landlord when filtering by building
		private void EnsureOwnedBuilding(int landlordId, int buildingId)
		{
			// Check Buildings table through property landlord id
			bool ok = _db.Buildings.Any(b => b.Id == buildingId && b.Property.LandlordId == landlordId);
			// Reject foreign-owned building filter
			if (!ok)
			{
				throw new UnauthorizedAccessException("Tòa nhà không thuộc về bạn.");
			}
		}
	}
}
