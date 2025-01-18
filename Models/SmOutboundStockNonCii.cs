using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class SmOutboundStockNonCii
{
    public string DeliveryNumber { get; set; } = null!;

    public string OrderNumber { get; set; } = null!;

    public string MaterialNumber { get; set; } = null!;

    public string? MaterialDescription { get; set; }

    public string? TargetLocation { get; set; }

    public int DeliveredQuantity { get; set; }

    public DateTime OutboundDate { get; set; }

    public string? SentBy { get; set; }

    public string FkInboundStockNonCiiDeliveryNumber { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public virtual SmInboundStockNonCii FkInboundStockNonCiiDeliveryNumberNavigation { get; set; } = null!;
}
