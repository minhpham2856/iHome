using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class NotificationTemplate
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Subject { get; set; }

    public string Body { get; set; } = null!;

    public string? Placeholders { get; set; }

    public DateTime CreatedAt { get; set; }
}
