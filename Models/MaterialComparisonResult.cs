namespace StockManagementWebApi.Models
{
	public class MaterialComparisonResult
	{
		public string PoolName { get; set; }
		public string ExcelMaterialNumber { get; set; }
		public int? ExcelStatus { get; set; }
		public string DbMaterialNumber { get; set; }
		public int? newstock { get; set; }
		public int? usedstock { get; set; }
		public int? others { get; set; }
		public int? Damaged { get; set; }
		public int? BreakFix { get; set; }
	}
}
