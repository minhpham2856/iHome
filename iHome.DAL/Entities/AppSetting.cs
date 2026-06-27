using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class AppSetting
{
    public int Id { get; set; }

    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }
}
