using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class Invoice
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
