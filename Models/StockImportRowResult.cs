namespace StockManagementWebApi.Models
{
    public class StockImportRowResult
    {
        public int RowNumber { get; set; }
        public string MaterialNumber { get; set; }
        public string SerialNumber { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
