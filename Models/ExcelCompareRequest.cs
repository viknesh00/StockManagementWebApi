namespace StockManagementWebApi.Models
{
	public class ExcelCompareRequest
	{
		public IFormFile file { get; set; }
		public string UserName { get; set; }
	}
}
