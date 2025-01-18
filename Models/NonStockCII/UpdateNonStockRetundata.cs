namespace StockManagementWebApi.Models.NonStockCII
{
	public class UpdateNonStockRetundata
	{
		public string MaterialNumber { get; set; }
		public string OrderNumber { get; set; }
		public string ExistOrderNumber { get; set; }
		public string ReturnLocation { get; set; }
		public DateTime? Returndate { get; set; }
		public int? ReturnQuantity { get; set; }
		public string ReceivedBy { get; set; }
		public string RackLocation { get; set; }
		public string ReturnType { get; set; }

		public string Reason { get; set; }
	}
}
