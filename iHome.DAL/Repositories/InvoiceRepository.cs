using iHome.DAL.Entities;
using System;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class InvoiceRepository
	{
		private readonly IHomeDbContext _context;

		// build the context internally - no constructor injection by design
		public InvoiceRepository()
		{
			_context = new IHomeDbContext();
		}

		public List<Invoice> GetAll() => _context.Invoices.ToList();

		// sum of TotalAmount across invoices that are not yet paid
		public decimal OutstandingTotal(string paidStatus) =>
			_context.Invoices
				.Where(i => i.Status != paidStatus)
				.Sum(i => i.TotalAmount);

		// count of invoices that are past their due date and not yet paid
		// DueDate is a DateOnly, so compare it directly against a DateOnly today
		public int OverdueCount(string paidStatus)
		{
			var today = DateOnly.FromDateTime(DateTime.Now);
			return _context.Invoices
				.Count(i => i.Status != paidStatus && i.DueDate < today);
		}
	}
}
