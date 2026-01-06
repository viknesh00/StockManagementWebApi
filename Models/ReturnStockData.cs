namespace StockManagementWebApi.Models
{
    public class ReturnStockData
    {
        public string DeliveryNumber { get; set; } = null!;

        public string? OrderNumber { get; set; }

        public string? SerialNumber { get; set; }

        public string? LocationReturnedFrom { get; set; }

        public int? Quantity { get; set; }

        public string? ReturnedReason { get; set; }

        public string? ReturnType { get; set; }

        public DateTime? ReturnedDate { get; set; }

        public string? ReturnedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public string RackLocation { get; set; }
        public int ReturnStockCIIKey { get; set; }
    }
}
