namespace StockManagementWebApi.Models
{
	public class DashboardList
	{
		public int? newstock { get; set; }
		public int? usedstock { get; set; }
		public int? damagedstock { get; set; }
		public int? breakfixstock { get; set; }
		public int? Outwardstock { get; set; }
		public int? inhandstock { get; set; }
	}
}
