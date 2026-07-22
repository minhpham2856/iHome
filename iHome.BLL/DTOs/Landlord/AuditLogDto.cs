using System;

namespace iHome.BLL.DTOs.Landlord
{
	// One audit trail row for the landlord activity log grid (who changed what and when).
	public class AuditLogDto
	{
		// Audit record primary key.
		public int Id { get; set; }
		// UTC/local timestamp when the action occurred.
		public DateTime Timestamp { get; set; }
		// Formatted date/time string for display in the log grid.
		public string TimestampDisplay { get; set; } = string.Empty;
		// Full name of the user who performed the action.
		public string UserFullName { get; set; } = string.Empty;
		// Login username of the actor.
		public string Username { get; set; } = string.Empty;
		// Role of the actor at action time (Landlord, Manager, etc.).
		public string Role { get; set; } = string.Empty;
		// Verb describing the operation (Create, Update, Delete, Login, …).
		public string Action { get; set; } = string.Empty;
		// Entity or screen affected (Property, Room, Contract, …).
		public string ObjectName { get; set; } = string.Empty;
		// Free-text summary of what changed, shown in the detail column.
		public string Detail { get; set; } = string.Empty;
		// Serialized field values before the change; null when not applicable.
		public string? OldValue { get; set; }
		// Serialized field values after the change; null when not applicable.
		public string? NewValue { get; set; }
	}

	// Filter dropdown entry for narrowing the audit log by user or role.
	public class AuditFilterOptionDto
	{
		// User Id when filtering by a specific account; null for role-wide or "all" options.
		public int? Id { get; set; }
		// Role name when filtering by role instead of a single user.
		public string? Role { get; set; }
		// Label shown in the filter ComboBox.
		public string? Name { get; set; }

		// Prefer Name for display; fall back to Role when Name is empty.
		public string Display => !string.IsNullOrWhiteSpace(Name) ? Name! : (Role ?? string.Empty);
	}
}
