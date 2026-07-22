using iHome.DAL.Entities;
using System;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for Payment.
	public class PaymentRepository
	{
		private readonly IHomeDbContext _context;

		public PaymentRepository()
		{
			_context = new IHomeDbContext();
		}

		// Sum of Amount for payments recorded in the given month/year.
		public decimal SumForMonth(int year, int month)
		{
			return _context.Payments
				.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month)
				.Sum(p => p.Amount);
		}
	}
}
