﻿namespace StockManagementWebApi.Models.UserManagement
{
	public class AddUser
	{
		public string? UserCode { get; set; }
		public string? UserName { get; set; }
		public string? Email { get; set; }
		public string? UserType { get; set; }
		public string? AccessLevel { get; set; }
		public string? Password { get; set; }
		public string? TenentCode { get; set; }

	}
}



