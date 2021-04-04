using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaprFrontEnd.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DaprClient _daprClient;

        public IndexModel(DaprClient daprClient)
        {
            _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        }

        public async Task OnGet()
        {
            var forecasts = await _daprClient.InvokeMethodAsync<IEnumerable<WeatherForecast>>(
                HttpMethod.Get,
                "daprbackend",
                "weatherforecast");

            ViewData["WeatherForecastData"] = forecasts;
        }

        public async Task<IActionResult> OnPostClick()
        {
            var data = new WeatherData { Temprature = 20 };
            await _daprClient.PublishEventAsync<WeatherData>("pubsub", "weather", data);
            return RedirectToPage();
        }
    }
}