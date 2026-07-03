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

		public List<Building> GetBuildings(int managerId, int? propertyId = null) =>
			_context.Buildings
				.AsNoTracking()
				.Where(b =>
					b.ManagerId == managerId &&
					b.IsActive &&
					(!propertyId.HasValue || b.PropertyId == propertyId.Value))
				.OrderBy(b => b.Name)
				.ToList();

		public List<Room> GetRooms(int managerId, int? propertyId = null) =>
			_context.Rooms
				.AsNoTracking()
				.Include(r => r.Building)
				.Include(r => r.RoomType)
				.Include(r => r.Contracts)
					.ThenInclude(c => c.ContractTenants)
				.Where(r =>
					r.Building.ManagerId == managerId &&
					r.Building.IsActive &&
					(!propertyId.HasValue || r.Building.PropertyId == propertyId.Value))
				.OrderBy(r => r.Building.Name)
				.ThenBy(r => r.Floor)
				.ThenBy(r => r.RoomNumber)
				.ToList();

		public List<ContractTenant> GetContractTenants(int managerId, int? propertyId = null) =>
			_context.ContractTenants
				.AsNoTracking()
				.Include(ct => ct.Tenant)
				.Include(ct => ct.Contract)
					.ThenInclude(c => c.Room)
						.ThenInclude(r => r.Building)
				.Where(ct =>
					ct.Contract.Room.Building.ManagerId == managerId &&
					ct.Contract.Room.Building.IsActive &&
					(!propertyId.HasValue ||
					 ct.Contract.Room.Building.PropertyId == propertyId.Value))
				.OrderBy(ct => ct.Contract.Room.Building.Name)
				.ThenBy(ct => ct.Contract.Room.RoomNumber)
				.ThenBy(ct => ct.Tenant.FullName)
				.ToList();

		public List<Service> GetServices(int managerId, int? propertyId = null) =>
			_context.Services
				.AsNoTracking()
				.Include(s => s.Building)
				.Where(s =>
					s.Building.ManagerId == managerId &&
					s.Building.IsActive &&
					(!propertyId.HasValue || s.Building.PropertyId == propertyId.Value))
				.OrderBy(s => s.Building.Name)
				.ThenBy(s => s.ServiceName)
				.ToList();

		public int CountAssignedBuildings(int managerId, int? propertyId = null) =>
			_context.Buildings
				.AsNoTracking()
				.Count(b =>
					b.ManagerId == managerId &&
					b.IsActive &&
					(!propertyId.HasValue || b.PropertyId == propertyId.Value));

		public Dictionary<string, int> GetRoomStatusCounts(int managerId, int? propertyId = null) =>
			_context.Rooms
				.AsNoTracking()
				.Where(r =>
					r.Building.ManagerId == managerId &&
					r.Building.IsActive &&
					(!propertyId.HasValue || r.Building.PropertyId == propertyId.Value))
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
			int? propertyId = null) =>
			_context.ContractTenants
				.AsNoTracking()
				.Where(ct =>
					ct.Contract.Room.Building.ManagerId == managerId &&
					ct.Contract.Room.Building.IsActive &&
					ct.Contract.Status == activeContractStatus &&
					(!propertyId.HasValue ||
					 ct.Contract.Room.Building.PropertyId == propertyId.Value))
				.Select(ct => ct.TenantId)
				.Distinct()
				.Count();

		public Dictionary<string, int> GetContractStatusCounts(
			int managerId,
			int? propertyId = null) =>
			_context.Contracts
				.AsNoTracking()
				.Where(c =>
					c.Room.Building.ManagerId == managerId &&
					c.Room.Building.IsActive &&
					(!propertyId.HasValue || c.Room.Building.PropertyId == propertyId.Value))
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
			int? propertyId = null) =>
			_context.Contracts
				.AsNoTracking()
				.Count(c =>
					c.Room.Building.ManagerId == managerId &&
					c.Room.Building.IsActive &&
					c.Status == activeStatus &&
					c.EndDate >= fromDate &&
					c.EndDate <= toDate &&
					(!propertyId.HasValue || c.Room.Building.PropertyId == propertyId.Value));

		public int CountOverdueInvoices(
			int managerId,
			string paidStatus,
			DateOnly today,
			int? propertyId = null) =>
			_context.Invoices
				.AsNoTracking()
				.Count(i =>
					i.Contract.Room.Building.ManagerId == managerId &&
					i.Contract.Room.Building.IsActive &&
					i.Status != paidStatus &&
					i.DueDate < today &&
					(!propertyId.HasValue ||
					 i.Contract.Room.Building.PropertyId == propertyId.Value));

		public decimal GetOutstandingAmount(
			int managerId,
			string paidStatus,
			int? propertyId = null)
		{
			var unpaidInvoices = _context.Invoices
				.AsNoTracking()
				.Where(i =>
					i.Contract.Room.Building.ManagerId == managerId &&
					i.Contract.Room.Building.IsActive &&
					i.Status != paidStatus &&
					(!propertyId.HasValue ||
					 i.Contract.Room.Building.PropertyId == propertyId.Value));

			var invoiceTotal = unpaidInvoices
				.Select(i => (decimal?)i.TotalAmount)
				.Sum() ?? 0m;
			var paidTotal = _context.Payments
				.AsNoTracking()
				.Where(p =>
					p.Invoice.Contract.Room.Building.ManagerId == managerId &&
					p.Invoice.Contract.Room.Building.IsActive &&
					p.Invoice.Status != paidStatus &&
					(!propertyId.HasValue ||
					 p.Invoice.Contract.Room.Building.PropertyId == propertyId.Value))
				.Select(p => (decimal?)p.Amount)
				.Sum() ?? 0m;

			return invoiceTotal - paidTotal;
		}

		public Dictionary<int, decimal> GetMonthlyRevenue(
			int managerId,
			DateOnly startDate,
			DateOnly endDate,
			int? propertyId = null)
		{
			return _context.Payments
				.AsNoTracking()
				.Where(p =>
					p.Invoice.Contract.Room.Building.ManagerId == managerId &&
					p.Invoice.Contract.Room.Building.IsActive &&
					p.PaymentDate >= startDate &&
					p.PaymentDate < endDate &&
					(!propertyId.HasValue ||
					 p.Invoice.Contract.Room.Building.PropertyId == propertyId.Value))
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
