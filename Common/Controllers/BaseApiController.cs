using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Models;

namespace StockManagementWebApi.Common.Controllers
{
	/// <summary>
	/// Base class for every controller in this API. It supplies the single set of response
	/// helpers that build the standard <see cref="ApiResponse"/> envelope, so no controller has
	/// to construct one by hand.
	/// </summary>
	/// <remarks>
	/// The helpers that shadow <see cref="ControllerBase"/> members (<c>Created</c>,
	/// <c>NoContent</c>, <c>BadRequest</c>, <c>NotFound</c>, <c>Unauthorized</c>, <c>Conflict</c>)
	/// are declared <c>new</c> on purpose: inside a derived controller only the enveloped versions
	/// are reachable, which is what keeps the response format consistent.
	/// </remarks>
	[ApiController]
	public abstract class BaseApiController : ControllerBase
	{
		/// <summary>Correlation id for the request in flight, set by the correlation middleware.</summary>
		protected string CorrelationId => HttpContext?.TraceIdentifier ?? string.Empty;

		// ---------------------------------------------------------------------------------
		// Success responses
		// ---------------------------------------------------------------------------------

		/// <summary>200 OK with a payload.</summary>
		protected IActionResult Success(object? data = null, string message = "Request processed successfully.")
			=> Envelope(StatusCodes.Status200OK, message, data);

		/// <summary>201 Created. Sets the <c>Location</c> header when <paramref name="location"/> is supplied.</summary>
		protected new IActionResult Created(object? data = null, string message = "Resource created successfully.", string? location = null)
		{
			if (!string.IsNullOrWhiteSpace(location))
			{
				Response.Headers.Location = location;
			}

			return Envelope(StatusCodes.Status201Created, message, data);
		}

		/// <summary>200 OK for a successful update.</summary>
		protected IActionResult Updated(object? data = null, string message = "Resource updated successfully.")
			=> Envelope(StatusCodes.Status200OK, message, data);

		/// <summary>200 OK for a successful delete.</summary>
		protected IActionResult Deleted(object? data = null, string message = "Resource deleted successfully.")
			=> Envelope(StatusCodes.Status200OK, message, data);

		/// <summary>
		/// A genuine 204 with no body. HTTP forbids a payload here, so this is the one response
		/// that does not carry the envelope - use <see cref="Success"/> when a body is wanted.
		/// </summary>
		protected new IActionResult NoContent() => base.NoContent();

		// ---------------------------------------------------------------------------------
		// Error responses
		//
		// Prefer throwing the matching exception from Common.Exceptions - the global middleware
		// produces an identical envelope. These helpers exist for the cases where a controller
		// wants to return an error directly without unwinding the stack.
		// ---------------------------------------------------------------------------------

		/// <summary>400 Bad Request.</summary>
		protected new IActionResult BadRequest(string message = "The request is invalid.", IEnumerable<string>? errors = null)
			=> Envelope(StatusCodes.Status400BadRequest, message, errors);

		/// <summary>400 Bad Request carrying a structured list of validation failures.</summary>
		protected IActionResult ValidationError(IEnumerable<string> errors, string message = "Validation failed.")
			=> Envelope(StatusCodes.Status400BadRequest, message, errors);

		/// <summary>401 Unauthorized.</summary>
		protected new IActionResult Unauthorized(string message = "Authentication is required to access this resource.", IEnumerable<string>? errors = null)
			=> Envelope(StatusCodes.Status401Unauthorized, message, errors);

		/// <summary>403 Forbidden.</summary>
		protected IActionResult Forbidden(string message = "You do not have permission to perform this action.", IEnumerable<string>? errors = null)
			=> Envelope(StatusCodes.Status403Forbidden, message, errors);

		/// <summary>404 Not Found.</summary>
		protected new IActionResult NotFound(string message = "The requested resource was not found.", IEnumerable<string>? errors = null)
			=> Envelope(StatusCodes.Status404NotFound, message, errors);

		/// <summary>409 Conflict.</summary>
		protected new IActionResult Conflict(string message = "The request conflicts with the current state of the resource.", IEnumerable<string>? errors = null)
			=> Envelope(StatusCodes.Status409Conflict, message, errors);

		/// <summary>500 Internal Server Error.</summary>
		protected IActionResult InternalServerError(string message = "An unexpected error occurred while processing your request.", IEnumerable<string>? errors = null)
			=> Envelope(StatusCodes.Status500InternalServerError, message, errors);

		// ---------------------------------------------------------------------------------

		private IActionResult Envelope(int statusCode, string message, object? dataOrErrors)
		{
			var isSuccess = statusCode is >= 200 and < 300;

			var response = isSuccess
				? ApiResponse.Ok(dataOrErrors, message, statusCode, CorrelationId)
				: ApiResponse.Fail(message, statusCode, CorrelationId, dataOrErrors as IEnumerable<string>);

			return new ObjectResult(response) { StatusCode = statusCode };
		}
	}
}
