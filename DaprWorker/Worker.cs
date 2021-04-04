using Dapr.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;

namespace DaprWorker
{
    public class Worker : BackgroundService
    {
        private const string STORENAME = "statestore";
        private const string KEY = "tweetcount";
        private readonly ILogger<Worker> _logger;
        private readonly TwitterClient _appClient;
        private readonly DaprClient _daprClient;
        private readonly IConfiguration _configuration;

        private long _count = 0;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _daprClient = new DaprClientBuilder().Build();

            var appCredentials = new TwitterCredentials(_configuration["TwitterClient:ConsumerKey"],
                _configuration["TwitterClient:ConsumerSecret"],
                _configuration["TwitterClient:AccessToken"],
                _configuration["TwitterClient:AccessSecret"]);
            _appClient = new TwitterClient(appCredentials);


        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var counter = await _daprClient.GetStateAsync<long>(STORENAME, KEY);
                _logger.LogInformation("Current value : {value}", counter.ToString());

                var stream = _appClient.Streams.CreateFilteredStream();
                stream.AddTrack("dapr");
                stream.AddTrack("dotnet");
                stream.AddTrack("microservices");
                stream.MatchingTweetReceived += async (sender, args) =>
                {
                    if (!args.Tweet.IsRetweet)
                    {
                        _count += 1;
                        _logger.LogInformation(args.Tweet.Text);
                        await _daprClient.SaveStateAsync<long>(STORENAME, KEY, _count);
                    }

                    //TODO:Make logical
                    var data = new WeatherData { Temprature = 23 };
                    await _daprClient.PublishEventAsync<WeatherData>("pubsub", "weather", data);
                };

                await stream.StartMatchingAnyConditionAsync();



            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
    }
}
