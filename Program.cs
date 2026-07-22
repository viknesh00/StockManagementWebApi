using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StockManagementWebApi.Common.Auth;
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
// Authentication
// ---------------------------------------------------------------------------------

// Validated on first use and at startup, so a missing or too-short signing key fails the
// deployment instead of silently weakening every token.
builder.Services
	.AddOptions<JwtOptions>()
	.Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
	.ValidateDataAnnotations()
	.ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
	.AddAuthentication(options =>
	{
		options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	})
	.AddJwtBearer(options =>
	{
		options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
		options.SaveToken = false;

		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = jwtOptions.Issuer,

			ValidateAudience = true,
			ValidAudience = jwtOptions.Audience,

			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),

			ValidateLifetime = true,
			// Default is 5 minutes of grace, which would keep expired tokens working well past
			// their stated expiry. Zero means "expired" means expired.
			ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),

			// ClaimTypes.Name is what BaseApiController.CurrentUserName reads.
			NameClaimType = System.Security.Claims.ClaimTypes.Name,
			RoleClaimType = System.Security.Claims.ClaimTypes.Role
		};

		// Auth failures answer with the standard envelope rather than an empty challenge.
		options.Events = JwtBearerEventHandlers.Create();
	});

builder.Services.AddAuthorization();

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
			// Without this the browser hides both headers from JavaScript: the client needs
			// Token-Expired to decide whether to refresh, and X-Correlation-ID for support.
			policy.WithExposedHeaders(JwtBearerEventHandlers.TokenExpiredHeader, CorrelationIdMiddleware.HeaderName);
		});
});

builder.Services.AddControllers(options =>
{
	// Every endpoint requires a valid token unless it opts out with [AllowAnonymous].
	// Only LoginController does.
	options.Filters.Add(new AuthorizeFilter());
});

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
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo { Title = "Stock Management API", Version = "v1" });

	// Lets the Swagger UI "Authorize" button send a bearer token.
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Paste the access token returned by /api/Login/Login. The 'Bearer ' prefix is added for you."
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddDbContext<MydbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("MyDBConnection")));

// Application services. Scoped so each one shares the request's DbContext and is disposed
// with the request scope - no connection outlives the request that opened it.
// The notification publisher falls back to the token identity when an operation does not
// name the acting user in its payload.
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IUploadedFileStore, UploadedFileStore>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ISmCompanyService, SmCompanyService>();
builder.Services.AddScoped<ISmUserService, SmUserService>();
builder.Services.AddScoped<IInboundStockCiiService, InboundStockCiiService>();
builder.Services.AddScoped<IOutboundStockCiiService, OutboundStockCiiService>();
builder.Services.AddScoped<INonCiiStockService, NonCiiStockService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationPublisher, NotificationPublisher>();

var app = builder.Build();

// Warn loudly at boot if the refresh-token table is missing, instead of letting the first
// sign-in fail with an opaque 500.
await AuthSchemaCheck.VerifyRefreshTokenStoreAsync(
	app.Services,
	app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("StockManagementWebApi.Startup"));

// ---------------------------------------------------------------------------------
// Pipeline
//
// Order matters: the exception handler is outermost so that it also covers the middleware
// registered after it, and the correlation id is assigned before anything can fail.
// ---------------------------------------------------------------------------------
app.UseGlobalExceptionHandling();
app.UseCorrelationId();

// Responses that never reached a controller (unmatched route, 405) arrive here with an empty
// body; give them the standard envelope too. Authentication failures are already handled by
// the JWT bearer events, which write the envelope themselves.
app.UseStatusCodePages(async statusCodeContext =>
{
	var context = statusCodeContext.HttpContext;

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

	await ApiResponseWriter.WriteErrorAsync(context, statusCode, message);
});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors();

// Authentication must run before authorization: one establishes who the caller is, the
// other decides whether they are allowed in.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
