namespace StockManagementWebApi.Models
{
	public class StagingModel
	{
		public int? Id { get; set; }

		public string UserName { get; set; }

		public string MaterialNumber { get; set; }

		public string SerialNumber { get; set; }

		public string OrderNumber { get; set; }

		public string Type { get; set; }

		public string DeviceStatus { get; set; }

		public DateTime? QCDate { get; set; }

		public string QCBy { get; set; }
		public DateTime? Date { get; set; }
	}
}
