namespace StockManagementWebApi.Models
{
	public class UpdateInboundData
	{
		public string MaterialNumber { get; set; }
		public string SerialNumber { get; set; }
		public string ExistSerialNumber { get; set; }
		public string RackLocation { get; set; }
		public string DeliveryNumber { get; set; }
		public string OrderNumber { get; set; }
		public DateTime? InwardDate { get; set; }
		public string InwardFrom { get; set; }
		public string ReceivedBy { get; set; }


		
	}
}
