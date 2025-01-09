namespace StockManagementWebApi.Models
{
	public class AddStockInward
	{
		public string MaterialNumber { get; set; }
		public string MaterialDescription { get; set; }
		
		public string OrderNumber { get; set; }
		public DateTime? Inwarddate { get; set; }
		public string InwardFrom { get; set; }
		public string ReceivedBy { get; set; }
		public string RacKLocation { get; set; }
		public IFormFile file { get; set; }



	}
}
