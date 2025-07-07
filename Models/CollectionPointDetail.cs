namespace StockManagementWebApi.Models
{
	public class CollectionPointDetail
	{
		public string UserName { get; set; }
		public string MaterialNumber { get; set; }
		public string SerialNumber { get; set; }
		public string? RackLocation { get; set; }
		public string? CollectionPointerName { get; set; }
		public DateTime? CollectionPointDate { get; set; }
		public string? CollectionPointStatus { get; set; }

	}

	
}
