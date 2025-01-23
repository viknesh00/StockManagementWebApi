namespace StockManagementWebApi.Models.NonStockCII
{
    public class NonStockReturnCii
    {
        public string? MaterialNumber { get; set; }

        public string? DeliveryNumber { get; set; }

        public string? OrderNumber { get; set; }

        public string? LocationReturnedFrom { get; set; }

        public int? Qunatity { get; set; }

        public string? ReturnReason { get; set; }

        public string? ReturnType { get; set; }
        public string? RackLocation { get; set; }
        public DateTime? ReturnedDate { get; set; }

        public string? ReturnedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
