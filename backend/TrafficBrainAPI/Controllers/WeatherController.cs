using Microsoft.AspNetCore.Mvc;
using TrafficBrainAPI.Services;

namespace TrafficBrainAPI.Controllers
{
    [ApiController]
    [Route("api/weather")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        // GET: api/weather/all
        [HttpGet("all")]
        public async Task<ActionResult> GetAllCitiesWeather()
        {
            var weather = await _weatherService.GetAllCitiesWeatherAsync();
            return Ok(weather);
        }

        // GET: api/weather/Lilongwe
        [HttpGet("{city}")]
        public async Task<ActionResult> GetCityWeather(string city)
        {
            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather == null)
                return NotFound(new { error = $"Weather data not available for {city}" });
            return Ok(weather);
        }

        // GET: api/weather/signal/Lilongwe
        [HttpGet("signal/{city}")]
        public async Task<ActionResult> GetSignalAdjustment(string city)
        {
            var adjustment = await _weatherService.GetSignalAdjustmentAsync(city);
            return Ok(new
            {
                City = city,
                Adjustment = adjustment,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}