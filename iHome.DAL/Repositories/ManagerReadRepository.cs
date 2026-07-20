using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class ManagerReadRepository
	{
		private readonly IHomeDbContext _context;

		public ManagerReadRepository()
		{
			_context = new IHomeDbContext();
		}

		public List<Property> GetProperties(int managerId) =>
			_context.Buildings
				.AsNoTracking()
				.Where(b =>
					b.ManagerId == managerId &&
					b.IsActive &&
					b.Property.IsActive)
				.Select(b => b.Property)
				.Distinct()
				.OrderBy(p => p.Name)
				.ToList();

		public List<Building> GetBuildings(int managerId, int? buildingId = null) =>
			_context.Buildings
				.AsNoTracking()
				.Where(b =>
					b.ManagerId == managerId &&
					b.IsActive &&
					(!buildingId.HasValue || b.Id == buildingId.Value))
				.OrderBy(b => b.Name)
				.ToList();

		public List<Room> GetRooms(int managerId, int? buildingId = null) =>
			_context.Rooms
				.AsNoTracking()
				.Include(r => r.Building)
				.Include(r => r.RoomType)
				.Include(r => r.Contracts)
					.ThenInclude(c => c.ContractTenants)
				.Where(r =>
					r.Building.ManagerId == managerId &&
					r.Building.IsActive &&
					(!buildingId.HasValue || r.BuildingId == buildingId.Value))
				.OrderBy(r => r.Building.Name)
				.ThenBy(r => r.Floor)
				.ThenBy(r => r.RoomNumber)
				.ToList();

		public List<ContractTenant> GetContractTenants(int managerId, int? buildingId = null) =>
			_context.ContractTenants
				.AsNoTracking()
				.Include(ct => ct.Tenant)
				.Include(ct => ct.Contract)
					.ThenInclude(c => c.Room)
						.ThenInclude(r => r.Building)
				.Where(ct =>
					ct.Contract.Room.Building.ManagerId == managerId &&
					ct.Contract.Room.Building.IsActive &&
					(!buildingId.HasValue ||
					 ct.Contract.Room.BuildingId == buildingId.Value))
				.OrderBy(ct => ct.Contract.Room.Building.Name)
				.ThenBy(ct => ct.Contract.Room.RoomNumber)
				.ThenBy(ct => ct.Tenant.FullName)
				.ToList();

		public List<Service> GetServices(int managerId, int? buildingId = null)
		{
			var managedPropertyIds = _context.Buildings
				.AsNoTracking()
				.Where(b =>
					b.ManagerId == managerId &&
					b.IsActive &&
					(!buildingId.HasValue || b.Id == buildingId.Value))
				.Select(b => b.PropertyId)
				.Distinct();

			return _context.Services
				.AsNoTracking()
				.Include(s => s.Property)
				.Where(s =>
					managedPropertyIds.Contains(s.PropertyId) &&
					s.Property.IsActive)
				.OrderBy(s => s.Property.Name)
				.ThenBy(s => s.ServiceName)
				.ToList();
		}
		public int CountAssignedBuildings(int managerId, int? buildingId = null) =>
			_context.Buildings
				.AsNoTracking()
				.Count(b =>
					b.ManagerId == managerId &&
					b.IsActive &&
					(!buildingId.HasValue || b.Id == buildingId.Value));

		public Dictionary<string, int> GetRoomStatusCounts(int managerId, int? buildingId = null) =>
			_context.Rooms
				.AsNoTracking()
				.Where(r =>
					r.Building.ManagerId == managerId &&
					r.Building.IsActive &&
					(!buildingId.HasValue || r.BuildingId == buildingId.Value))
				.GroupBy(r => r.Status)
				.Select(group => new
				{
					Status = group.Key,
					Count = group.Count()
				})
				.ToList()
				.ToDictionary(
					item => item.Status,
					item => item.Count,
					StringComparer.OrdinalIgnoreCase);

		public int CountActiveTenants(
			int managerId,
			string activeContractStatus,
			int? buildingId = null) =>
			_context.ContractTenants
				.AsNoTracking()
				.Where(ct =>
					ct.Contract.Room.Building.ManagerId == managerId &&
					ct.Contract.Room.Building.IsActive &&
					ct.Contract.Status == activeContractStatus &&
					(!buildingId.HasValue ||
					 ct.Contract.Room.BuildingId == buildingId.Value))
				.Select(ct => ct.TenantId)
				.Distinct()
				.Count();

		public Dictionary<string, int> GetContractStatusCounts(
			int managerId,
			int? buildingId = null) =>
			_context.Contracts
				.AsNoTracking()
				.Where(c =>
					c.Room.Building.ManagerId == managerId &&
					c.Room.Building.IsActive &&
					(!buildingId.HasValue || c.Room.BuildingId == buildingId.Value))
				.GroupBy(c => c.Status)
				.Select(group => new
				{
					Status = group.Key,
					Count = group.Count()
				})
				.ToList()
				.ToDictionary(
					item => item.Status,
					item => item.Count,
					StringComparer.OrdinalIgnoreCase);

		public int CountExpiringContracts(
			int managerId,
			string activeStatus,
			DateOnly fromDate,
			DateOnly toDate,
			int? buildingId = null) =>
			_context.Contracts
				.AsNoTracking()
				.Count(c =>
					c.Room.Building.ManagerId == managerId &&
					c.Room.Building.IsActive &&
					c.Status == activeStatus &&
					c.EndDate >= fromDate &&
					c.EndDate <= toDate &&
					(!buildingId.HasValue || c.Room.BuildingId == buildingId.Value));

		public int CountOverdueInvoices(
			int managerId,
			string paidStatus,
			DateOnly today,
			int? buildingId = null) =>
			_context.Invoices
				.AsNoTracking()
				.Count(i =>
					i.Contract.Room.Building.ManagerId == managerId &&
					i.Contract.Room.Building.IsActive &&
					i.Status != paidStatus &&
					i.DueDate < today &&
					(!buildingId.HasValue ||
					 i.Contract.Room.BuildingId == buildingId.Value));

		public decimal GetOutstandingAmount(
			int managerId,
			string paidStatus,
			int? buildingId = null)
		{
			var unpaidInvoices = _context.Invoices
				.AsNoTracking()
				.Where(i =>
					i.Contract.Room.Building.ManagerId == managerId &&
					i.Contract.Room.Building.IsActive &&
					i.Status != paidStatus &&
					(!buildingId.HasValue ||
					 i.Contract.Room.BuildingId == buildingId.Value));

			var invoiceTotal = unpaidInvoices
				.Select(i => (decimal?)i.TotalAmount)
				.Sum() ?? 0m;
			var paidTotal = _context.Payments
				.AsNoTracking()
				.Where(p =>
					p.Invoice.Contract.Room.Building.ManagerId == managerId &&
					p.Invoice.Contract.Room.Building.IsActive &&
					p.Invoice.Status != paidStatus &&
					(!buildingId.HasValue ||
					 p.Invoice.Contract.Room.BuildingId == buildingId.Value))
				.Select(p => (decimal?)p.Amount)
				.Sum() ?? 0m;

			return invoiceTotal - paidTotal;
		}

		public Dictionary<int, decimal> GetMonthlyRevenue(
			int managerId,
			DateOnly startDate,
			DateOnly endDate,
			int? buildingId = null)
		{
			return _context.Payments
				.AsNoTracking()
				.Where(p =>
					p.Invoice.Contract.Room.Building.ManagerId == managerId &&
					p.Invoice.Contract.Room.Building.IsActive &&
					p.PaymentDate >= startDate &&
					p.PaymentDate < endDate &&
					(!buildingId.HasValue ||
					 p.Invoice.Contract.Room.BuildingId == buildingId.Value))
				.GroupBy(p => new
				{
					p.PaymentDate.Year,
					p.PaymentDate.Month
				})
				.Select(group => new
				{
					Key = group.Key.Year * 100 + group.Key.Month,
					Amount = group.Sum(p => p.Amount)
				})
				.ToDictionary(item => item.Key, item => item.Amount);
		}
	}
}
