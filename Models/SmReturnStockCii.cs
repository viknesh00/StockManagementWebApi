using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class SmReturnStockCii
{
    public string DeliveryNumber { get; set; } = null!;

    public string? OrderNumber { get; set; }

    public string? SerialNumber { get; set; }

    public string? LocationReturnedFrom { get; set; }

    public int? Quantity { get; set; }

    public string? ReturnedReason { get; set; }

    public string? ReturnType { get; set; }

    public DateTime? ReturnedDate { get; set; }

    public string? ReturnedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }
}
