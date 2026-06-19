using System.Text.Json;
using TrafficBrainAPI.Models;
using TrafficBrainAPI.Data;

namespace TrafficBrainAPI.Services
{
    public interface IWeatherService
    {
        Task<WeatherData?> GetWeatherAsync(string city);
        Task<List<WeatherData>> GetAllCitiesWeatherAsync();
        Task<string> GetSignalAdjustmentAsync(string city);
    }

    public class WeatherData
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string ConditionIcon { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public double Visibility { get; set; }
        public string WeatherImpact { get; set; } = string.Empty;
        public string SignalAdjustment { get; set; } = string.Empty;
        public int RecommendedGreenExtension { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }

    public class WeatherService : IWeatherService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        // Malawian cities with traffic lights
        private readonly List<string> _cities = new()
        {
            "Lilongwe", "Blantyre", "Mzuzu", "Zomba"
        };

        public WeatherService(IConfiguration config, HttpClient httpClient, AppDbContext context)
        {
            _config = config;
            _httpClient = httpClient;
            _context = context;
        }

        public async Task<WeatherData?> GetWeatherAsync(string city)
        {
            try
            {
                var apiKey = _config["WeatherApi:ApiKey"];
                var baseUrl = _config["WeatherApi:BaseUrl"];

                if (string.IsNullOrEmpty(apiKey))
                    return GetFallbackWeather(city);

                var url = $"{baseUrl}/weather?q={city},MW&appid={apiKey}&units=metric";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return GetFallbackWeather(city);

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var condition = root.GetProperty("weather")[0].GetProperty("main").GetString() ?? "Clear";
                var temp = root.GetProperty("main").GetProperty("temp").GetDouble();
                var humidity = root.GetProperty("main").GetProperty("humidity").GetDouble();
                var windSpeed = root.GetProperty("wind").GetProperty("speed").GetDouble();
                var visibility = root.TryGetProperty("visibility", out var vis)
                    ? vis.GetDouble() / 1000 : 10.0;

                var impact = CalculateWeatherImpact(condition, visibility, windSpeed);
                var adjustment = CalculateSignalAdjustment(condition, visibility);
                var greenExtension = CalculateGreenExtension(condition, visibility);
                var icon = GetWeatherIcon(condition);

                var weatherData = new WeatherData
                {
                    City = city,
                    Country = "Malawi",
                    Condition = condition,
                    ConditionIcon = icon,
                    Temperature = Math.Round(temp, 1),
                    Humidity = humidity,
                    WindSpeed = Math.Round(windSpeed * 3.6, 1), // convert m/s to km/h
                    Visibility = Math.Round(visibility, 1),
                    WeatherImpact = impact,
                    SignalAdjustment = adjustment,
                    RecommendedGreenExtension = greenExtension,
                    RecordedAt = DateTime.UtcNow
                };

                // Save to database
                _context.WeatherConditions.Add(new WeatherCondition
                {
                    DistrictId = GetDistrictId(city),
                    Condition = condition,
                    Temperature = temp,
                    Humidity = humidity,
                    WindSpeed = windSpeed,
                    Visibility = visibility,
                    WeatherImpact = impact,
                    RecordedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                return weatherData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Weather API error for {city}: {ex.Message}");
                return GetFallbackWeather(city);
            }
        }

        public async Task<List<WeatherData>> GetAllCitiesWeatherAsync()
        {
            var results = new List<WeatherData>();
            foreach (var city in _cities)
            {
                var weather = await GetWeatherAsync(city);
                if (weather != null)
                    results.Add(weather);
                await Task.Delay(200); // Rate limiting
            }
            return results;
        }

        public async Task<string> GetSignalAdjustmentAsync(string city)
        {
            var weather = await GetWeatherAsync(city);
            return weather?.SignalAdjustment ?? "Normal operation";
        }

        private string CalculateWeatherImpact(string condition, double visibility, double windSpeed)
        {
            if (condition == "Thunderstorm" || visibility < 1)
                return "Critical";
            if (condition == "Rain" || condition == "Drizzle" || visibility < 3)
                return "High";
            if (condition == "Fog" || condition == "Mist" || windSpeed > 50)
                return "Medium";
            if (condition == "Clouds" || windSpeed > 30)
                return "Low";
            return "None";
        }

        private string CalculateSignalAdjustment(string condition, double visibility)
        {
            if (condition == "Thunderstorm")
                return "Extend all green phases by 15s — severe storm reducing visibility";
            if (condition == "Rain" || condition == "Drizzle")
                return "Extend green phases by 8s — rain reducing vehicle speeds";
            if (condition == "Fog" || condition == "Mist" || visibility < 3)
                return "Extend green phases by 10s — fog reducing visibility";
            if (condition == "Clouds")
                return "Extend green phases by 3s — overcast conditions";
            return "Normal signal timing — clear conditions";
        }

        private int CalculateGreenExtension(string condition, double visibility)
        {
            if (condition == "Thunderstorm") return 15;
            if (condition == "Rain" || condition == "Drizzle") return 8;
            if (condition == "Fog" || visibility < 3) return 10;
            if (condition == "Clouds") return 3;
            return 0;
        }

        private string GetWeatherIcon(string condition)
        {
            return condition switch
            {
                "Clear" => "☀️",
                "Clouds" => "⛅",
                "Rain" => "🌧️",
                "Drizzle" => "🌦️",
                "Thunderstorm" => "⛈️",
                "Snow" => "❄️",
                "Fog" => "🌫️",
                "Mist" => "🌫️",
                "Haze" => "🌁",
                _ => "🌤️"
            };
        }

        private int GetDistrictId(string city)
        {
            return city switch
            {
                "Lilongwe" => 1,
                "Blantyre" => 2,
                "Mzuzu" => 3,
                "Zomba" => 4,
                _ => 1
            };
        }

        private WeatherData GetFallbackWeather(string city)
        {
            return new WeatherData
            {
                City = city,
                Country = "Malawi",
                Condition = "Clear",
                ConditionIcon = "☀️",
                Temperature = 25.0,
                Humidity = 60,
                WindSpeed = 15,
                Visibility = 10,
                WeatherImpact = "None",
                SignalAdjustment = "Normal signal timing — clear conditions",
                RecommendedGreenExtension = 0,
                RecordedAt = DateTime.UtcNow
            };
        }
    }
}