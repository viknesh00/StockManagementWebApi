namespace StockManagementWebApi.Models
{
    public class CompanyList
    {
        public string pk_CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? DomainName { get; set; }
        public bool? CompanyStatus { get; set; }
    }
}
