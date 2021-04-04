using System;

namespace DaprFrontEnd
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF { get; set; }

        public string Summary { get; set; }
    }

    public class WeatherData
    {
        public double Temprature { get; set; }
    }
}