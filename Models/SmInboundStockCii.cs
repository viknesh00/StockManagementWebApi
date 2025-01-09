using System;
using System.Collections.Generic;

namespace StockManagementWebApi.Models;

public partial class SmInboundStockCii
{
    public string DeliveryNumber { get; set; } = null!;

    public string OrderNumber { get; set; } = null!;

    public string? SerialNumber { get; set; }

    public string? MaterialNumber { get; set; }

    public string? MaterialDescription { get; set; }

    public DateTime? InwardDate { get; set; }

    public string? RackLocation { get; set; }

    public string? SourceLocation { get; set; }

    public int? Quantity { get; set; }

    public string? ReceivedBy { get; set; }

    public string? Status { get; set; }

    public int? DeliveredQuantity { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? UpdatedBy { get; set; }

    public int FkUserCode { get; set; }
	public int NewStock { get; set; }
	public int UsedStock { get; set; }

	public virtual SmUser FkUserCodeNavigation { get; set; } = null!;

    public virtual ICollection<SmOutboundStockCii> SmOutboundStockCiis { get; set; } = new List<SmOutboundStockCii>();
}
