using iHome.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class ContractRepository
	{
		private readonly IHomeDbContext _context;

		// build the context internally - no constructor injection by design
		public ContractRepository()
		{
			_context = new IHomeDbContext();
		}

		public List<Contract> GetAll() => _context.Contracts.ToList();

		// count contracts whose Status equals the given value (e.g. "Active", "Expired", "Terminated")
		public int CountByStatus(string status) =>
			_context.Contracts.Count(c => c.Status == status);

		// Hợp đồng Active còn hiệu lực dưới 1 tháng (tính theo tháng lịch)
		public List<Contract> ExpiringSoon(string status, int days)
		{
			DateOnly today = DateOnly.FromDateTime(DateTime.Today);
			DateOnly limit = today.AddMonths(1);
			return _context.Contracts
				.Where(c => c.Status == status && c.EndDate >= today && c.EndDate < limit)
				.ToList();
		}
	}
}
