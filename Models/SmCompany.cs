using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class SmCompany
{
    public string PkCompanyCode { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string? CompanyLocation { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public string? CompanyStatus { get; set; }

    public virtual ICollection<SmTenent> SmTenents { get; set; } = new List<SmTenent>();
}
