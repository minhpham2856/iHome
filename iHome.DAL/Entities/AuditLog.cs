using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class AuditLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Action { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual User User { get; set; } = null!;
}
