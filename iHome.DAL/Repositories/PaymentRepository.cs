using iHome.DAL.Entities;
using System;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Payment.
	public class PaymentRepository
	{
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		// build the context internally - no constructor injection by design
		public PaymentRepository()
		{
			// Instantiate a dedicated DbContext for this repository instance
			_context = new IHomeDbContext();
		}

		// sum of Amount for payments recorded in the given month/year
		public decimal SumForMonth(int year, int month)
		{
			// Filter payments by CreatedAt year and month, then aggregate the Amount column
			return _context.Payments
				.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month)
				.Sum(p => p.Amount);
		}
	}
}
