namespace StockManagementWebApi.Models
{
	public class AddSingleStockInward
	{

		public string MaterialNumber { get; set; }
		public string MaterialDescription { get; set; }
		public string? DeliveryNumber { get; set; }
		public string? OrderNumber { get; set; }
		public DateTime? Inwarddate { get; set; }
		public string? InwardFrom { get; set; }
		public string? ReceivedBy { get; set; }
		public string? RacKLocation { get; set; }
		public string? SerialNumber { get; set; }
		public int? Quantity { get; set; }
        public string? Status { get; set; }
		public string UserName { get; set; }

	}
}
