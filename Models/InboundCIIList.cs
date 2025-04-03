namespace StockManagementWebApi.Models
{
	public class InboundCIIList
	{
		public string MaterialNumber { get; set; }
		public string MaterialDescription { get; set; }
		public string SerialNumber { get; set; }
		public string? RackLocation { get; set; }
		public string? DeliveryNumber { get; set; }
		public string? OrderNumber { get; set; }
		public DateTime? InwardDate { get; set; }
		public string? InwardFrom { get; set; }
		
	}
}


