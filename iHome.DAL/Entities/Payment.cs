using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class Payment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public DateOnly PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    public int ReceivedBy { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual User ReceivedByNavigation { get; set; } = null!;
}
