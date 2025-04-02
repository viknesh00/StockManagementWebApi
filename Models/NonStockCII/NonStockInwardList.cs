namespace StockManagementWebApi.Models.NonStockCII
{
	public class NonStockInwardList
	{
		public string? DeliveryNumber { get; set; }

		public string? OrderNumber { get; set; }

		//public string? SerialNumber { get; set; }

		public string? MaterialNumber { get; set; }

		public string? MaterialDescription { get; set; }

		public DateTime? InwardDate { get; set; }

		public string? RackLocation { get; set; }

		public string? SourceLocation { get; set; }

		public int? Quantity { get; set; }

		public string? ReceivedBy { get; set; }

		public string? Status { get; set; }

		public int? DeliveredQuantity { get; set; }

		public DateTime? CreatedDate { get; set; }

		public DateTime? UpdatedDate { get; set; }

		public string? UpdatedBy { get; set; }

		public int? Fk_UserCode { get; set; }
		public int? NewStock { get; set; }
		public int? UsedStock { get; set; }

	}
}
