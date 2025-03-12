namespace StockManagementWebApi.Models
{
	public class DashboardDeliveryCount
	{
		public int? CIIDeliveryCount { get; set; }
		public int? CIIReturnCount { get; set; }
		public int? NonCIIDeliveryCount { get; set; }
		public int? NonCIIReturnCount { get; set; }
	}
}
