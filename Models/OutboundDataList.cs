namespace StockManagementWebApi.Models
{
	public class OutboundDataList
	{
		public string MaterialNumber { get; set; }
		public string SerialNumber { get; set; }
		
		
		
		public string? ReceiverName { get; set; }
		
		public string OutBoundOrderNumber { get; set; }
		public DateTime? OutBoundDate { get; set; }
		public string TargetLocation { get; set; }
		public string sentby { get; set; }


		

	}
	
}


