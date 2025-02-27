namespace StockManagementWebApi.Models.UserManagement
{
	public class UpdateUser
	{
		public int? UserCode { get; set; }
		public string? UserName { get; set; }
		public bool? UserStatus { get; set; }
		public string? UserType { get; set; }
		public string? AccessLevel { get; set; }
		
		
	}
}
