namespace StockManagementWebApi.Models
{
	public class AddDeliveryData
	{
		public string UserName { get; set; }
		public string? DeliveryNumber { get; set; }
		public string MaterialNumber { get; set; }
		public List<string> SerialNumber { get; set; }
		public string MaterialDescription { get; set; }
		public string? OrderNumber { get; set; }
		public DateTime? OutBounddate { get; set; }
		public string? TargetLocation { get; set; }
		public string? SentBy { get; set; }
		public List<string>? Fk_Inbound_StockCII_DeliveryNumber { get; set; }
		public string? ReceiverName { get; set; }
	}
}

