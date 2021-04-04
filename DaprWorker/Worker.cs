using Dapr;
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

        private const string KEY_DAPR = "dapr";
        private const string KEY_MICROSERVICES = "microservices";
        private const string KEY_DOTNET = "dotnet";

        private readonly ILogger<Worker> _logger;
        private readonly TwitterClient _appClient;
        private readonly DaprClient _daprClient;
        private readonly IConfiguration _configuration;

        private readonly string[] _trackKeyWords = new string[] { KEY_DAPR, KEY_DOTNET, KEY_MICROSERVICES };

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
                var stream = _appClient.Streams.CreateFilteredStream();

                foreach (var item in _trackKeyWords)
                {
                    stream.AddTrack(item);
                }

                stream.MatchingTweetReceived += async (sender, args) =>
                {
                    if (!args.Tweet.IsRetweet)
                    {
                        _logger.LogInformation(args.Tweet.Text);

                        foreach (var item in _trackKeyWords)
                        {
                            if (args.MatchingTracks.Contains(item))
                            {
                                var count = await _daprClient.GetStateAsync<long>(STORENAME, item);
                                count += 1;
                                await _daprClient.SaveStateAsync<long>(STORENAME, item, count);
                            }
                        }

                        //TODO:Make logical
                        var data = new WeatherData { Temprature = 23 };
                        await _daprClient.PublishEventAsync<WeatherData>("pubsub", "weather", data);

                    }
                };
                await stream.StartMatchingAnyConditionAsync();
            }
            catch (DaprException dex)
            {
                _logger.LogError(dex, dex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,ex.Message);
            }
            finally
            {

            }
        }
    }
}
