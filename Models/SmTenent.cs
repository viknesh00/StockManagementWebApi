using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class SmTenent
{
    public string PkTenentCode { get; set; } = null!;

    public string TenentLocation { get; set; } = null!;

    public string? FkCompanyCode { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public string? TenentStatus { get; set; }

    public virtual SmCompany? FkCompanyCodeNavigation { get; set; }

    public virtual ICollection<SmUser> SmUsers { get; set; } = new List<SmUser>();
}
