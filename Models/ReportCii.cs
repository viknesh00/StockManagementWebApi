using System.Data;

namespace StockManagementWebApi.Models
{
	public class ReportCii
	{
		public string MaterialNumber { get; set; }
		public string SerialNumber { get; set; }
		public int? MaterialKey { get; set; }
		public string status { get; set; }
		
		public DateTime? InwardDate { get; set; }
		public DateTime? OutboundDate { get; set; }
	}
}
