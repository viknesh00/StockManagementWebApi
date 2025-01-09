namespace StockManagementWebApi.Models
{
	public class AddReturnDataList
	{
		public string DeliveryNumber { get; set; }
		public string MaterialNumber { get; set; }
		public string MaterialDescription { get; set; }

		public string SerialNumber { get; set; }
		public string OrderNumber { get; set; }
		public string LocationReturnedFrom { get; set; }
		public DateTime Returneddate { get; set; }
		public string ReturnedBy { get; set; }
		public string RackLocation { get; set; }
		public string ReturnType { get; set; }

		public string Returns { get; set; }
	}
	
}
