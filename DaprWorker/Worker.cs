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

            var consumerOnlyCredentials = new ConsumerOnlyCredentials(_configuration["TwitterClient:ConsumerKey"],
                _configuration["TwitterClient:ConsumerSecret"]);
            _appClient = new TwitterClient(consumerOnlyCredentials);

        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var counter = await _daprClient.GetStateAsync<int>(STORENAME, KEY);
                _logger.LogInformation("Current value : {value}", counter.ToString());

                Tweetinvi.TweetinviEvents.SubscribeToClientEvents(_appClient);
                await _appClient.Auth.InitializeClientBearerTokenAsync();
                var authenticatedUser = await _appClient.Users.GetAuthenticatedUserAsync();
                _logger.LogInformation(authenticatedUser.Name);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
    }
}
