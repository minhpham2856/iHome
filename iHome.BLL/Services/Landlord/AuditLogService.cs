using iHome.BLL.DTOs.Landlord;
using iHome.DAL.Entities;
using iHome.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace iHome.BLL.Services
{
	public class LandlordAuditLogService
	{
		private readonly AuditLogRepository _logs;
		private readonly UserRepository _users;

		public LandlordAuditLogService() : this(new IHomeDbContext())
		{
		}

		public LandlordAuditLogService(IHomeDbContext context)
		{
			_logs = new AuditLogRepository(context);
			_users = new UserRepository(context);
		}

		public List<AuditFilterOptionDto> GetRoleOptions() =>
			new()
			{
				new() { Role = null, Name = "Tất cả vai trò" },
				new() { Role = "Landlord", Name = "Chủ trọ" },
				new() { Role = "Manager", Name = "Nhân viên" }
			};

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
			if (user == null || user.Role != "Landlord")
			{
				throw new UnauthorizedAccessException("Chỉ chủ trọ mới xem nhật ký hệ thống.");
			}
		}

		private static AuditLogDto Map(AuditLog log) => new()
		{
			Id = log.Id,
			Timestamp = log.Timestamp,
			TimestampDisplay = log.Timestamp.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
			UserFullName = log.User?.FullName ?? "—",
			Username = log.User?.Username ?? "—",
			Role = log.User?.Role ?? string.Empty,
			RoleDisplay = FormatRole(log.User?.Role),
			Action = log.Action,
			TableName = log.TableName,
			RecordId = log.RecordId,
			OldValue = log.OldValue,
			NewValue = log.NewValue
		};

		private static string FormatRole(string? role) => role switch
		{
			"Landlord" => "Chủ trọ",
			"Manager" => "Nhân viên",
			_ => role ?? "—"
		};
	}
}
