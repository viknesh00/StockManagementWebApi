namespace StockManagementWebApi.Models.LoginModel
{
	public class ResetPassword
	{
		public string Email { get; set; }
		public string ExistPassword { get; set; }
		public string NewPassword { get; set; }
	}
}
