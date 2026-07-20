using iHome.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iHome.DAL.Repositories
{
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

		public void Add(
			int userId,
			string action,
			string tableName,
			string? recordId,
			string? oldValue,
			string? newValue)
		{
			_context.AuditLogs.Add(new AuditLog
			{
				UserId = userId,
				Action = action,
				TableName = tableName,
				RecordId = recordId,
				OldValue = oldValue,
				NewValue = newValue,
				Timestamp = DateTime.Now
			});
			_context.SaveChanges();
		}

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
