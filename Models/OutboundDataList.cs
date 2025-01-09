﻿namespace StockManagementWebApi.Models
{
	public class OutboundDataList
	{
		public string MaterialNumber { get; set; }
		public string SerialNumber { get; set; }
		public string RackLocation { get; set; }
		public string DeliveryNumber { get; set; }
		public string OrderNumber { get; set; }
		public DateTime? InwardDate { get; set; }
		//public string MaterialName { get; set; }
		public string ReceivedBy { get; set; }
		public string OutBoundOrderNumber { get; set; }
		public DateTime? OutBoundDate { get; set; }
		public string TargetLocation { get; set; }
		public string sentby { get; set; }


		

	}
}


