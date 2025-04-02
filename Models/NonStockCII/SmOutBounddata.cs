namespace StockManagementWebApi.Models.NonStockCII
{
	public class SmOutBounddata
	{
		public string DeliveryNumber { get; set; } = null!;

		//public string? OrderNumber { get; set; } = null!;

		//public string MaterialNumber { get; set; } = null!;

		//public string? MaterialDescription { get; set; }

		//public string? TargetLocation { get; set; }

		public int DeliveredQuantity { get; set; }

		public int InboundStockNonCIIKey { get; set; }

		////public DateTime OutboundDate { get; set; }

		//public string? SentBy { get; set; }

		////public string Fk_InboundStock_NonCII_DeliveryNumber { get; set; } = null!;

		//public DateTime? CreatedDate { get; set; }

		//public DateTime? UpdatedDate { get; set; }

		//public string? UpdatedBy { get; set; }

		//public bool? IsActive { get; set; }
		//public string? ReceiverName { get; set; }
	}
}
