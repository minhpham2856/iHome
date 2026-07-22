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
	// Landlord audit log viewer — role/user filters and AuditDiff display mapping
	public class LandlordAuditLogService
	{
		private readonly AuditLogRepository _logs;
		private readonly UserRepository _users;

		public LandlordAuditLogService() : this(new IHomeDbContext()) { }

		public LandlordAuditLogService(IHomeDbContext context)
		{
			_logs = new AuditLogRepository(context);
			_users = new UserRepository(context);
		}

		// Role filter: all / Landlord / Manager
		public List<AuditFilterOptionDto> GetRoleOptions()
		{
			return new List<AuditFilterOptionDto>
			{
				new() { Name = "Tất cả vai trò" },
				new() { Role = UserRole.Landlord },
				new() { Role = UserRole.Manager }
			};
		}

		// User filter — users who appear in logs, optionally scoped by role
		public List<AuditFilterOptionDto> GetUserOptions(string? role = null)
		{
			var options = new List<AuditFilterOptionDto>
			{
				new() { Id = null, Name = "Tất cả người dùng" }
			};
			options.AddRange(_logs.GetUsersForFilter(role)
				.Select(u => new AuditFilterOptionDto
				{
					Id = u.Id,
					Role = u.Role,
					Name = $"{u.FullName} ({u.Username})"
				}));
			return options;
		}

		// Filtered log list — landlord callers only
		public List<AuditLogDto> GetLogs(int landlordId, string? role, int? userId)
		{
			EnsureLandlord(landlordId);
			return _logs.GetFiltered(role, userId)
				.Select(Map)
				.ToList();
		}

		private void EnsureLandlord(int landlordId)
		{
			var user = _users.GetById(landlordId);
			if (user == null || user.Role != UserRole.Landlord)
			{
				throw new UnauthorizedAccessException("Chỉ chủ trọ mới xem nhật ký hệ thống.");
			}
		}

		// Map entity → grid DTO; AuditDiff.ToDisplay pretty-prints Old/New columns
		private static AuditLogDto Map(AuditLog log)
		{
			return new AuditLogDto
			{
				Id = log.Id,
				Timestamp = log.Timestamp,
				TimestampDisplay = log.Timestamp.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
				UserFullName = log.User?.FullName ?? "—",
				Username = log.User?.Username ?? "—",
				Role = log.User?.Role ?? string.Empty,
				Action = log.Action ?? string.Empty,
				ObjectName = string.IsNullOrWhiteSpace(log.TableName) ? "—" : log.TableName,
				Detail = string.IsNullOrWhiteSpace(log.Detail) ? "—" : log.Detail,
				OldValue = AuditDiff.ToDisplay(log.OldValue),
				NewValue = AuditDiff.ToDisplay(log.NewValue)
			};
		}
	}
}
