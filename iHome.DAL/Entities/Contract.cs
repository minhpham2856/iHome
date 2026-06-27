using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class Contract
{
    public int Id { get; set; }

    public string ContractCode { get; set; } = null!;

    public int RoomId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal MonthlyRent { get; set; }

    public decimal DepositAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? TerminationReason { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ContractTenant> ContractTenants { get; set; } = new List<ContractTenant>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Room Room { get; set; } = null!;
}
