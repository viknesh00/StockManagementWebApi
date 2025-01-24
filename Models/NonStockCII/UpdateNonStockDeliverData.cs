namespace StockManagementWebApi.Models.NonStockCII
{
	public class UpdateNonStockDeliverData
	{

		public string MaterialNumber { get; set; }
		public string MaterialDescription { get; set; }
		public string DeliveryNumber { get; set; }
		public string OrderNumber { get; set; }
		public string ExistDeliveryNumber { get; set; }
		public string ExistOrderNumber { get; set; }
		public int ExistDeliveredQuantity { get; set; }
		public DateTime? OutboundDate { get; set; }
		public string ReceiverName { get; set; }
		public int DeliveredQuantity { get; set; }
		public string TargetLocation { get; set; }
		public string SentBy { get; set; }
		public string DeliveryNumber_inbound { get; set; }
	}
}
