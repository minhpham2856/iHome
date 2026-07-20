using System.Collections.Generic;

namespace iHome.BLL.DTOs.Landlord
{
	public class AuditLogDto
	{
		public int Id { get; set; }
		public DateTime Timestamp { get; set; }
		public string TimestampDisplay { get; set; } = string.Empty;
		public string UserFullName { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
		public string RoleDisplay { get; set; } = string.Empty;
		public string Action { get; set; } = string.Empty;
		public string TableName { get; set; } = string.Empty;
		public string? RecordId { get; set; }
		public string? OldValue { get; set; }
		public string? NewValue { get; set; }
	}

	public class AuditFilterOptionDto
	{
		public int? Id { get; set; }
		public string? Role { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}
