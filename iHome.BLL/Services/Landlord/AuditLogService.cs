using iHome.BLL.Common;
using iHome.BLL.DTOs.Landlord;
using iHome.BLL.Enums;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.BLL.Services
{
	// Landlord views system audit logs — filter by role/user, map entity to grid DTO
	public class LandlordAuditLogService
	{
		private readonly AuditLogRepository _logs;
		private readonly UserRepository _users;

		public LandlordAuditLogService() : this(new IHomeDbContext()) { }

		public LandlordAuditLogService(IHomeDbContext context)
		{
			// Wire audit log repository against the shared DbContext
			_logs = new AuditLogRepository(context);
			// Wire user repository for landlord guard checks
			_users = new UserRepository(context);
		}

		// Role filter combo: All + Landlord + Manager
		public List<AuditFilterOptionDto> GetRoleOptions()
		{
			// Build fixed list of role filter entries for the UI dropdown
			var options = new List<AuditFilterOptionDto>
			{
				// Sentinel row meaning "no role filter"
				new() { Name = "Tất cả vai trò" },
				// Restrict grid to landlord actions only
				new() { Role = UserRole.Landlord },
				// Restrict grid to manager actions only
				new() { Role = UserRole.Manager }
			};
			// Return combo items bound by the audit log page
			return options;
		}

		// User filter combo — optionally pre-filter users by role
		public List<AuditFilterOptionDto> GetUserOptions(string? role = null)
		{
			// Start with "all users" sentinel option
			var options = new List<AuditFilterOptionDto>
			{
				new() { Id = null, Name = "Tất cả người dùng" }
			};
			// Load distinct users who appear in audit logs, optionally scoped by role
			var usersForFilter = _logs.GetUsersForFilter(role);
			// Map each user row to a filter option with display name "FullName (Username)"
			options.AddRange(usersForFilter
				.Select(u => new AuditFilterOptionDto
				{
					Id = u.Id,
					Role = u.Role,
					Name = $"{u.FullName} ({u.Username})"
				}));
			// Return full combo list for the user filter dropdown
			return options;
		}

		// Filtered log list — only callable by a verified landlord
		public List<AuditLogDto> GetLogs(int landlordId, string? role, int? userId)
		{
			// Reject callers who are not active landlords
			EnsureLandlord(landlordId);
			// Query audit rows with optional role/user filters, then map to display DTOs
			var logs = _logs.GetFiltered(role, userId);
			// Project each entity through Map and materialize as a list
			return logs
				.Select(Map)
				.ToList();
		}

		// Guard: caller must be a Landlord user
		private void EnsureLandlord(int landlordId)
		{
			// Load the caller from Users table
			var user = _users.GetById(landlordId);
			// Missing user or wrong role → unauthorized
			if (user == null || user.Role != UserRole.Landlord)
			{
				throw new UnauthorizedAccessException("Chỉ chủ trọ mới xem nhật ký hệ thống.");
			}
		}

		// Map AuditLog entity to grid DTO (format timestamp, AuditDiff.ToDisplay for Old/New)
		private static AuditLogDto Map(AuditLog log)
		{
			// Build one row for the audit log DataGrid
			return new AuditLogDto
			{
				Id = log.Id,
				Timestamp = log.Timestamp,
				// Human-readable timestamp for Vietnamese UI
				TimestampDisplay = log.Timestamp.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
				// Fallback em dash when navigation property User is missing
				UserFullName = log.User?.FullName ?? "—",
				Username = log.User?.Username ?? "—",
				Role = log.User?.Role ?? string.Empty,
				Action = log.Action ?? string.Empty,
				// TableName column shown as "Object" in the grid
				ObjectName = string.IsNullOrWhiteSpace(log.TableName) ? "—" : log.TableName,
				Detail = string.IsNullOrWhiteSpace(log.Detail) ? "—" : log.Detail,
				// Pretty-print JSON diff columns for before/after values
				OldValue = AuditDiff.ToDisplay(log.OldValue),
				NewValue = AuditDiff.ToDisplay(log.NewValue)
			};
		}
	}
}
