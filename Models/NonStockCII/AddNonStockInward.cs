namespace StockManagementWebApi.Models.NonStockCII
{
	public class AddNonStockInward
	{
		public string MaterialNumber { get; set; }
		public string MaterialDescription { get; set; }
		public string DeliveryNumber { get; set; }
		public string OrderNumber { get; set; }
		public DateTime? Inwarddate { get; set; }
		public string InwardFrom { get; set; }
		public int QuantityReceived { get; set; }
		public string ReceivedBy { get; set; }
		public string RacKLocation { get; set; }
		
	}
}
