namespace StockManagementWebApi.Models
{
    public class Log_record
    {
        public string user_Id { get; set; }
        public DateTime modified_date { get; set; }
        public string? serialNumber { get; set; }
        public string? marterialNumber { get; set; }
        public string? Activities { get; set; }
        public string? Exist_record { get; set; }
        public string? New_record { get; set; }

    }

}
