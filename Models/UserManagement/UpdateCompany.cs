namespace StockManagementWebApi.Models.UserManagement
{
	public class UpdateCompany
	{
		public string CompanyId { get; set; }
		public string ExistCompanyId { get; set; }
		public string? CompanyName { get; set; }
		public string? DomainName { get; set; }
	}
}
