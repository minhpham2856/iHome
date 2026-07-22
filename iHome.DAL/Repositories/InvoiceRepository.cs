using iHome.DAL.Entities;
using System;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Invoice.
	public class InvoiceRepository
	{
		private readonly IHomeDbContext _context;

		public InvoiceRepository()
		{
			_context = new IHomeDbContext();
		}

		// All invoices.
		public List<Invoice> GetAll()
		{
			return _context.Invoices.ToList();
		}

		// Sum of TotalAmount across invoices that are not yet paid.
		public decimal OutstandingTotal(string paidStatus)
		{
			return _context.Invoices
				.Where(i => i.Status != paidStatus)
				.Sum(i => i.TotalAmount);
		}

		// Count unpaid invoices past DueDate (DateOnly vs today's DateOnly).
		public int OverdueCount(string paidStatus)
		{
			var today = DateOnly.FromDateTime(DateTime.Now);
			return _context.Invoices
				.Count(i => i.Status != paidStatus && i.DueDate < today);
		}
	}
}
