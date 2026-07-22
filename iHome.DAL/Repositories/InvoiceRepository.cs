using iHome.DAL.Entities;
using System;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Invoice.
	public class InvoiceRepository
	{
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		// build the context internally - no constructor injection by design
		public InvoiceRepository()
		{
			// Instantiate a dedicated DbContext for this repository instance
			_context = new IHomeDbContext();
		}

		// Query All records.
		public List<Invoice> GetAll()
		{
			// Load every invoice row from the database without additional filters or includes
			return _context.Invoices.ToList();
		}

		// sum of TotalAmount across invoices that are not yet paid
		public decimal OutstandingTotal(string paidStatus)
		{
			// Sum TotalAmount for invoices whose Status is anything other than the fully paid status label
			return _context.Invoices
				.Where(i => i.Status != paidStatus)
				.Sum(i => i.TotalAmount);
		}

		// count of invoices that are past their due date and not yet paid
		// DueDate is a DateOnly, so compare it directly against a DateOnly today
		public int OverdueCount(string paidStatus)
		{
			// Capture today's calendar date as DateOnly for a fair comparison with invoice DueDate values
			var today = DateOnly.FromDateTime(DateTime.Now);
			// Count unpaid invoices whose due date is strictly before today
			return _context.Invoices
				.Count(i => i.Status != paidStatus && i.DueDate < today);
		}
	}
}
