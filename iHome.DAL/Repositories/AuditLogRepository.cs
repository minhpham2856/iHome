using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
	// EF Core data access for AuditLog.
	public class AuditLogRepository
	{
		private readonly IHomeDbContext _context;

		public AuditLogRepository() : this(new IHomeDbContext())
		{
		}

		public AuditLogRepository(IHomeDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert an audit row; stamp Timestamp when caller left it default.
		public void Add(AuditLog log)
		{
			ArgumentNullException.ThrowIfNull(log);
			if (log.Timestamp == default)
			{
				log.Timestamp = DateTime.Now;
			}
			_context.AuditLogs.Add(log);
			_context.SaveChanges();
		}

		// Newest audit entries, optionally filtered by user role and/or user id.
		public List<AuditLog> GetFiltered(string? role, int? userId, int take = 500)
		{
			var query = _context.AuditLogs
				.AsNoTracking()
				.Include(a => a.User)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(role))
			{
				query = query.Where(a => a.User.Role == role);
			}
			if (userId.HasValue)
			{
				query = query.Where(a => a.UserId == userId.Value);
			}

			return query
				.OrderByDescending(a => a.Timestamp)
				.ThenByDescending(a => a.Id)
				.Take(take)
				.ToList();
		}

		// Users for filter dropdowns, optionally narrowed by role.
		public List<User> GetUsersForFilter(string? role = null)
		{
			var query = _context.Users.AsNoTracking().AsQueryable();
			if (!string.IsNullOrWhiteSpace(role))
			{
				query = query.Where(u => u.Role == role);
			}
			return query
				.OrderBy(u => u.Role)
				.ThenBy(u => u.FullName)
				.ToList();
		}
	}
}
