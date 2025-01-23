using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class ReturnStockNonCii
{
    public string MaterialNumber { get; set; } = null;

    public string DeliveryNumber { get; set; } = null!;

    public string? OrderNumber { get; set; }

    public string? LocationReturnedFrom { get; set; }

    public int? Qunatity { get; set; }

    public string? ReturnReason { get; set; }

    public string? ReturnType { get; set; }
    public string? RackLocation { get; set; }
    public DateTime? ReturnedDate { get; set; }

    public string? ReturnedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }
}
