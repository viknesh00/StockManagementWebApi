using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class SmUser
{
    public int PkUserCode { get; set; }

    public string LoginId { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? LastName { get; set; }

    public string Password { get; set; } = null!;

    public string FkTenentCode { get; set; } = null!;

    public string? FkRoleCode { get; set; }

    public string? UserStatus { get; set; }

    public DateTime? LastLogin { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual SmUserRole? FkRoleCodeNavigation { get; set; }

    public virtual SmTenent FkTenentCodeNavigation { get; set; } = null!;

    public virtual ICollection<SmInboundStockCii> SmInboundStockCiis { get; set; } = new List<SmInboundStockCii>();

    public virtual ICollection<SmInboundStockNonCii> SmInboundStockNonCiis { get; set; } = new List<SmInboundStockNonCii>();
}
