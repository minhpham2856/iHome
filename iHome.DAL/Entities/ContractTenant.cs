using System;
using System.Collections.Generic;

namespace iHome.DAL.Entities;

public partial class ContractTenant
{
    public int ContractId { get; set; }

    public int TenantId { get; set; }

    public bool IsMainTenant { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
