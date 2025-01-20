namespace StockManagementWebApi.Models
{
	public class UpdatedeliveryDataList
	{
		public string MaterialNumber { get; set; }
		public string SerialNumber { get; set; }
		public string OrderNumber { get; set; }
		public string ExistOrderNumber { get; set;}
		public DateTime? Outbounddate { get; set; }
		public string TargetLocation { get; set; }
		public string SentBy { get; set; }
		public string ReceiverName { get; set; }

	}
}
