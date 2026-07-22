using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;
using StockManagementWebApi.Models;
using StockManagementWebApi.Services;

namespace StockManagementWebApi.Controllers
{
	[Route("api/[controller]")]
	public class SmUsersController : BaseApiController
	{
		private readonly ISmUserService _userService;

		public SmUsersController(ISmUserService userService)
		{
			_userService = userService;
		}

		// GET: api/SmUsers
		[HttpGet]
		public async Task<IActionResult> GetSmUsers(CancellationToken cancellationToken)
		{
			var users = await _userService.GetAllAsync(cancellationToken);

			return Success(users, "Users retrieved successfully.");
		}

		// GET: api/SmUsers/5
		[HttpGet("{id}")]
		public async Task<IActionResult> GetSmUser(int id, CancellationToken cancellationToken)
		{
			var user = await _userService.GetByIdAsync(id, cancellationToken);

			return Success(user, "User retrieved successfully.");
		}

		// PUT: api/SmUsers/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutSmUser(int id, SmUser smUser, CancellationToken cancellationToken)
		{
			await _userService.UpdateAsync(id, smUser, cancellationToken);

			return Updated(message: "User updated successfully.");
		}

		// POST: api/SmUsers
		[HttpPost]
		public async Task<IActionResult> PostSmUser(SmUser smUser, CancellationToken cancellationToken)
		{
			var created = await _userService.CreateAsync(smUser, cancellationToken);

			return Created(created, "User created successfully.", Url.Action(nameof(GetSmUser), new { id = created.PkUserCode }));
		}

		// DELETE: api/SmUsers/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteSmUser(int id, CancellationToken cancellationToken)
		{
			await _userService.DeleteAsync(id, cancellationToken);

			return Deleted(message: "User deleted successfully.");
		}
	}
}
