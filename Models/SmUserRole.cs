using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class SmUserRole
{
    public string PkRoleCode { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public string? RoleStatus { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual ICollection<SmUser> SmUsers { get; set; } = new List<SmUser>();
}
