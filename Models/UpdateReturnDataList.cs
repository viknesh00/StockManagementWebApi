namespace StockManagementWebApi.Models
{
	public class UpdateReturnDataList
	{

		public string MaterialNumber { get; set; }
		public string SerialNumber { get; set; }
		public string OrderNumber { get; set; }
        public string ExistOrderNumber { get; set; }
        public string? LocationReturnedFrom { get; set; }
		public string? ReturnedDate { get; set; }

		public string? RackLocation { get; set; }

		public string? ReturnType { get; set; }
		public string? ReturnedBy { get; set; }
		public string? Returns { get; set; }
	}
	
}
