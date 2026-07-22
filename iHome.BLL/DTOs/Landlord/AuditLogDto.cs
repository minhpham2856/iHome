using System;

namespace iHome.BLL.DTOs.Landlord
{
	// One audit trail row for the landlord activity log grid (who changed what and when).
	public class AuditLogDto
	{
		public int Id { get; set; }
		public DateTime Timestamp { get; set; }
		public string TimestampDisplay { get; set; } = string.Empty;
		public string UserFullName { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
		public string Action { get; set; } = string.Empty;
		public string ObjectName { get; set; } = string.Empty;
		public string Detail { get; set; } = string.Empty;
		public string? OldValue { get; set; }
		public string? NewValue { get; set; }
	}

	// Filter dropdown entry for narrowing the audit log by user or role.
	public class AuditFilterOptionDto
	{
		public int? Id { get; set; }
		public string? Role { get; set; }
		public string? Name { get; set; }

		public string Display => !string.IsNullOrWhiteSpace(Name) ? Name! : (Role ?? string.Empty);
	}
}
