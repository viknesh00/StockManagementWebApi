namespace StockManagementWebApi.Models.UserManagement
{
	public class UserList
	{
		public int? UserCode { get; set; }
		public string? UserName { get; set; }
		public string? Email { get; set; }
		public string? UserType { get; set; }
		public string? AccessLevel { get; set; }
		public bool? UserStatus { get; set; }
		public bool? IsActive { get; set; }
	}
}
