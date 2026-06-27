using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class NotificationLog
{
    public int Id { get; set; }

    public int? RecipientId { get; set; }

    public string? RecipientEmail { get; set; }

    public string? RecipientPhone { get; set; }

    public string Type { get; set; } = null!;

    public string? Subject { get; set; }

    public string Content { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public DateTime SentAt { get; set; }

    public virtual Tenant? Recipient { get; set; }
}
