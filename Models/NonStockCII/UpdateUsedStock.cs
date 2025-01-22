namespace StockManagementWebApi.Models.NonStockCII
{
	public class UpdateUsedStock
	{
		public string OrderNumber { get; set; }
		public string ExistOrderNumber { get; set; }
		public string MaterialNumber { get; set; }
		public string ReturnLocation { get; set; }
		public DateTime? ReturnDate { get; set; }
		public int ItemQuantity { get; set; }
	}
}
