using Microsoft.AspNetCore.Mvc;
using StockManagementWebApi.Common.Controllers;

namespace StockManagementWebApi.Controllers
{
	[Route("[controller]")]
	public class WeatherForecastController : BaseApiController
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		private readonly ILogger<WeatherForecastController> _logger;

		public WeatherForecastController(ILogger<WeatherForecastController> logger)
		{
			_logger = logger;
		}

		[HttpGet(Name = "GetWeatherForecast")]
		public IActionResult Get()
		{
			var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = Summaries[Random.Shared.Next(Summaries.Length)]
			})
			.ToArray();

			return Success(forecast, "Weather forecast retrieved successfully.");
		}
	}
}
