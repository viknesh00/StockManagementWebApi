using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManagementWebApi.Common.Files;
using StockManagementWebApi.Common.Models;
using StockManagementWebApi.Middleware;
using StockManagementWebApi.Models;
using StockManagementWebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
	// Scopes carry the correlation id, request path and method set by CorrelationIdMiddleware.
	options.IncludeScopes = true;
	options.SingleLine = false;
	options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
	options.UseUtcTimestamp = true;
});
builder.Logging.AddDebug();

// ---------------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(
		policy =>
		{
			policy.AllowAnyOrigin();
			policy.AllowAnyHeader();
			policy.AllowAnyMethod();
		});
});

builder.Services.AddControllers();

// Model-binding and data-annotation failures must use the same envelope as everything else,
// instead of the default ValidationProblemDetails body.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.InvalidModelStateResponseFactory = context =>
	{
		var errors = context.ModelState
			.Where(entry => entry.Value?.Errors.Count > 0)
			.SelectMany(entry => entry.Value!.Errors.Select(error =>
				string.IsNullOrWhiteSpace(entry.Key)
					? error.ErrorMessage
					: $"{entry.Key}: {error.ErrorMessage}"))
			.Where(message => !string.IsNullOrWhiteSpace(message))
			.ToList();

		var payload = ApiResponse.Fail(
			"Validation failed.",
			StatusCodes.Status400BadRequest,
			context.HttpContext.TraceIdentifier,
			errors);

		return new BadRequestObjectResult(payload);
	};
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MydbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("MyDBConnection")));

// Application services. Scoped so each one shares the request's DbContext and is disposed
// with the request scope - no connection outlives the request that opened it.
builder.Services.AddSingleton<IUploadedFileStore, UploadedFileStore>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ISmCompanyService, SmCompanyService>();
builder.Services.AddScoped<ISmUserService, SmUserService>();
builder.Services.AddScoped<IInboundStockCiiService, InboundStockCiiService>();
builder.Services.AddScoped<IOutboundStockCiiService, OutboundStockCiiService>();
builder.Services.AddScoped<INonCiiStockService, NonCiiStockService>();

var app = builder.Build();

// ---------------------------------------------------------------------------------
// Pipeline
//
// Order matters: the exception handler is outermost so that it also covers the middleware
// registered after it, and the correlation id is assigned before anything can fail.
// ---------------------------------------------------------------------------------
app.UseGlobalExceptionHandling();
app.UseCorrelationId();

// Responses that never reached a controller (unmatched route, 405, 401/403 from the auth
// middleware) arrive here with an empty body; give them the standard envelope too.
app.UseStatusCodePages(async statusCodeContext =>
{
	var context = statusCodeContext.HttpContext;
	if (context.Response.HasStarted)
	{
		return;
	}

	var statusCode = context.Response.StatusCode;
	var message = statusCode switch
	{
		StatusCodes.Status401Unauthorized => "Authentication is required to access this resource.",
		StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
		StatusCodes.Status404NotFound => "The requested resource was not found.",
		StatusCodes.Status405MethodNotAllowed => "The HTTP method is not supported for this resource.",
		StatusCodes.Status415UnsupportedMediaType => "The request content type is not supported.",
		StatusCodes.Status429TooManyRequests => "Too many requests. Please try again later.",
		>= 500 => "An unexpected error occurred while processing your request.",
		_ => "The request could not be completed."
	};

	context.Response.ContentType = "application/json; charset=utf-8";

	var payload = ApiResponse.Fail(message, statusCode, context.TraceIdentifier);
	await context.Response.WriteAsync(
		JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
		context.RequestAborted);
});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();

app.Run();
