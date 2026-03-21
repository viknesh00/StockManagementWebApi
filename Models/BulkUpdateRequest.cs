namespace StockManagementWebApi.Models
{
    public class BulkUpdateRequest
    {
        public string MaterialNumber { get; set; }
        public List<string> SerialNumbers { get; set; }

        public string? RackLocation { get; set; }
        public string? Status { get; set; }
    }
}
