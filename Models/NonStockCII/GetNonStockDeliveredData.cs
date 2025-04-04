namespace StockManagementWebApi.Models.NonStockCII
{
    public class GetNonStockDeliveredData
    {
        public string DeliveryNumber { get; set; }

        public string OrderNumber { get; set; } 

        public string MaterialNumber { get; set; } 

        public string? MaterialDescription { get; set; }

        public string? TargetLocation { get; set; }

        public int DeliveredQuantity { get; set; }

        public DateTime? OutboundDate { get; set; }

        public string? SentBy { get; set; }

        public string Fk_InboundStock_NonCii_DeliveryNumber { get; set; } 

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public bool? IsActive { get; set; }
        public string? ReceiverName { get; set; }
		public int OutboundStockNonCIIKey { get; set; }
	}
}
