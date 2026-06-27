using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class Tenant
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string IdCardNumber { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string? PermanentAddress { get; set; }

    public string? IdCardImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ContractTenant> ContractTenants { get; set; } = new List<ContractTenant>();

    public virtual ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
}
