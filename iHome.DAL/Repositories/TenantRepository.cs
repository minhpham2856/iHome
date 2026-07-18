using iHome.DAL.Entities;
using System;
using System.Linq;

namespace iHome.DAL.Repositories
{
	public class TenantRepository
	{
		private readonly IHomeDbContext _context;

		// build the context internally — no constructor injection by design
		public TenantRepository()
		{
			_context = new IHomeDbContext();
		}

		// total number of tenants in the database
		public int Count() => _context.Tenants.Count();
	}
}
