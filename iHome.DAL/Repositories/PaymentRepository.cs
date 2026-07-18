using iHome.DAL.Entities;
using System;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class PaymentRepository
	{
		private readonly IHomeDbContext _context;

		// build the context internally — no constructor injection by design
		public PaymentRepository()
		{
			_context = new IHomeDbContext();
		}

		// sum of Amount for payments recorded in the given month/year
		public decimal SumForMonth(int year, int month) =>
			_context.Payments
				.Where(p => p.CreatedAt.Year == year && p.CreatedAt.Month == month)
				.Sum(p => p.Amount);
	}
}
