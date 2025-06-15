namespace StockManagementWebApi.Models
{
	public class StockCiiList
	{
		public int? newstock { get; set; }
		public int? usedstock { get; set; }
		public int? others { get; set; }
		public string materialNumber { get; set; }
		public string MaterialDescription { get; set; }
		public int? Damaged { get; set; }
		public int? BreakFix { get; set; }
	}
}
