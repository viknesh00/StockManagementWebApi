using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class SmOutboundStockCii
{
    public string? DeliveryNumber { get; set; } = null!;

    public string? OrderNumber { get; set; }

    public string? SerialNumber { get; set; }

    public string? MaterialNumber { get; set; }

    public string? MaterialDescription { get; set; }

    public string? TargetLocation { get; set; }

    public int? DeliveredQuantity { get; set; }

    public DateTime? OutboundDate { get; set; }

    public string? SentBy { get; set; }

    public string? FkInboundStockCiiDeliveryNumber { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

	

	public virtual SmInboundStockCii FkInboundStockCiiDeliveryNumberNavigation { get; set; } = null!;
}
