namespace StockManagementWebApi.Models.NonStockCII
{
    public class AddNonCIIStockInward
    {
        public IFormFile file { get; set; }

        public string UserName { get; set; }

        public string DeliveryNumber { get; set; }

        public string OrderNumber { get; set; }

        public string MaterialDescription { get; set; }

        public DateTime? InwardDate { get; set; }

        public string InwardFrom { get; set; }

        public int QuantityReceived { get; set; }

        public string ReceivedBy { get; set; }

        public string RackLocation { get; set; }

        public string PoNumber { get; set; }

        public string Location { get; set; }

        public string? Status { get; set; }
    }
}
