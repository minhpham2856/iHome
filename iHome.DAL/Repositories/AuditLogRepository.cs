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
		// Shared EF Core context instance.
		private readonly IHomeDbContext _context;

		public AuditLogRepository() : this(new IHomeDbContext())
		{
		}

		public AuditLogRepository(IHomeDbContext context)
		{
			// Reject a null context so every repository method has a valid DbContext to query against
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		// Insert and save.
		public void Add(AuditLog log)
		{
			// Guard against inserting a null entity which would corrupt the audit trail
			ArgumentNullException.ThrowIfNull(log);
			// When the caller did not set Timestamp, stamp the row with the current local time before persistence
			if (log.Timestamp == default)
			{
				log.Timestamp = DateTime.Now;
			}
			// Stage the audit log entity in the EF change tracker for insert
			_context.AuditLogs.Add(log);
			// Commit the new audit row to the database immediately
			_context.SaveChanges();
		}

		// Query Filtered records.
		public List<AuditLog> GetFiltered(string? role, int? userId, int take = 500)
		{
			// Start from AuditLogs with read-only tracking, eager-load the acting User, and keep the query composable
			var query = _context.AuditLogs
				.AsNoTracking()
				.Include(a => a.User)
				.AsQueryable();

			// When a role filter is supplied, restrict rows to users whose Role column matches exactly
			if (!string.IsNullOrWhiteSpace(role))
			{
				query = query.Where(a => a.User.Role == role);
			}
			// When a user id filter is supplied, restrict rows to actions performed by that specific user
			if (userId.HasValue)
			{
				query = query.Where(a => a.UserId == userId.Value);
			}

			// Materialize the newest audit entries first, break ties by Id descending, and cap the result size
			return query
				.OrderByDescending(a => a.Timestamp)
				.ThenByDescending(a => a.Id)
				.Take(take)
				.ToList();
		}

		// Query UsersForFilter records.
		public List<User> GetUsersForFilter(string? role = null)
		{
			// Build a read-only Users query that can optionally be narrowed by role
			var query = _context.Users.AsNoTracking().AsQueryable();
			// Apply role filter when the UI passes a non-empty role string
			if (!string.IsNullOrWhiteSpace(role))
			{
				query = query.Where(u => u.Role == role);
			}
			// Return users sorted by role then full name for stable dropdown ordering
			return query
				.OrderBy(u => u.Role)
				.ThenBy(u => u.FullName)
				.ToList();
		}
	}
}
