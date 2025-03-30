using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using OtelWebApi.Models;

namespace OtelWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly Meter _meter = new("WeatherForecast");
        private static readonly Counter<int> _requestCounter = _meter.CreateCounter<int>("weather_forecast_requests");

        public WeatherForecastController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            // Track requests with custom metric
            _requestCounter.Add(1, new KeyValuePair<string, object?>("path", Request.Path));

            using var activity = Activity.Current?.Source.StartActivity("GenerateWeatherForecast");
            activity?.SetTag("sample.tag", "custom-value");

            try
            {
                // Simulate external API call
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://jsonplaceholder.typicode.com/todos/1");
                response.EnsureSuccessStatusCode();

                var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();

                activity?.SetStatus(ActivityStatusCode.Ok);
                return forecast;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }

    
}
