using Xunit;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Test.AspNetCore.MaxConcurrentRequests.Extensions;
using Demo.AspNetCore.MaxConcurrentRequests;
using Demo.AspNetCore.MaxConcurrentRequests.Middlewares;

namespace Test.AspNetCore.MaxConcurrentRequests
{
    public class MaxConcurrentRequestsTests
    {
        #region Types
        private struct HttpResponseInformation
        {
            public HttpStatusCode StatusCode { get; set; }

            public TimeSpan Timing { get; set; }

            public override string ToString()
            {
                return $"StatusCode: {StatusCode} | Timing {Timing}";
            }
        }
        #endregion

        #region Fields
        private const string DEFAULT_RESPONSE = "-- Demo.AspNetCore.MaxConcurrentConnections --";

        private const int SOME_CONCURRENT_REQUESTS_COUNT = 30;
        private const int SOME_MAX_CONCURRENT_REQUESTS_LIMIT = 10;
        private const int SOME_MAX_QUEUE_LENGTH = 10;
        private const int TIME_SHORTER_THAN_PROCESSING = 300;
        #endregion

        #region Prepare SUT
        private TestServer PrepareTestServer(IEnumerable<KeyValuePair<string, string>> configuration = null)
        {
            IWebHostBuilder webHostBuilder = new WebHostBuilder()
                .UseStartup<Startup>();

            if (configuration != null)
            {
                ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddInMemoryCollection(configuration);
                IConfiguration buildedConfiguration = configurationBuilder.Build();

                webHostBuilder.UseConfiguration(buildedConfiguration);
                webHostBuilder.ConfigureServices((services) =>
                {
                    services.Configure<MaxConcurrentRequestsOptions>(options => buildedConfiguration.GetSection("MaxConcurrentRequestsOptions").Bind(options));
                });
            }

            return new TestServer(webHostBuilder);
        }
        #endregion

        #region Tests
        [Fact]
        public async Task SingleRequest_ReturnsSuccessfulResponse()
        {
            using (TestServer server = PrepareTestServer())
            {
                using (HttpClient client = server.CreateClient())
                {
                    HttpResponseMessage response = await client.GetAsync("/");

                    Assert.True(response.IsSuccessStatusCode);
                }
            }
        }

        [Fact]
        public async Task SingleRequest_ReturnsDefaultResponse()
        {
            using (TestServer server = PrepareTestServer())
            {
                using (HttpClient client = server.CreateClient())
                {
                    HttpResponseMessage response = await client.GetAsync("/");
                    string responseText = await response.Content.ReadAsStringAsync();

                    Assert.Equal(DEFAULT_RESPONSE, responseText);
                }
            }
        }

        [Fact]
        public void SomeMaxConcurrentRequestsLimit_Drop_SomeConcurrentRequestsCount_CountMinusLimitRequestsReturnServiceUnavailable()
        {
            Dictionary<string, string> configuration = new Dictionary<string, string>
            {
                {"MaxConcurrentRequestsOptions:Limit", SOME_MAX_CONCURRENT_REQUESTS_LIMIT.ToString() },
                {"MaxConcurrentRequestsOptions:LimitExceededPolicy", MaxConcurrentRequestsLimitExceededPolicy.Drop.ToString() }
            };

            HttpResponseInformation[] responseInformation = GetResponseInformation(configuration, SOME_CONCURRENT_REQUESTS_COUNT);

            Assert.Equal(SOME_CONCURRENT_REQUESTS_COUNT - SOME_MAX_CONCURRENT_REQUESTS_LIMIT, responseInformation.Count(i => i.StatusCode == HttpStatusCode.ServiceUnavailable));
        }

        [Fact]
        public void SomeMaxConcurrentRequestsLimit_FifoQueueDropTail_SomeMaxQueueLength_SomeConcurrentRequestsCount_CountMinusLimitRequestsAndMaxQueueLengthReturnServiceUnavailable()
        {
            Dictionary<string, string> configuration = new Dictionary<string, string>
            {
                {"MaxConcurrentRequestsOptions:Limit", SOME_MAX_CONCURRENT_REQUESTS_LIMIT.ToString() },
                {"MaxConcurrentRequestsOptions:LimitExceededPolicy", MaxConcurrentRequestsLimitExceededPolicy.FifoQueueDropTail.ToString() },
                {"MaxConcurrentRequestsOptions:MaxQueueLength", SOME_MAX_QUEUE_LENGTH.ToString() }
            };

            HttpResponseInformation[] responseInformation = GetResponseInformation(configuration, SOME_CONCURRENT_REQUESTS_COUNT);

            Assert.Equal(SOME_CONCURRENT_REQUESTS_COUNT - SOME_MAX_CONCURRENT_REQUESTS_LIMIT - SOME_MAX_QUEUE_LENGTH, responseInformation.Count(i => i.StatusCode == HttpStatusCode.ServiceUnavailable));
        }

        [Fact]
        public void SomeMaxConcurrentRequestsLimit_FifoQueueDropHead_SomeMaxQueueLength_SomeConcurrentRequestsCount_CountMinusLimitRequestsAndMaxQueueLengthReturnServiceUnavailable()
        {
            Dictionary<string, string> configuration = new Dictionary<string, string>
            {
                {"MaxConcurrentRequestsOptions:Limit", SOME_MAX_CONCURRENT_REQUESTS_LIMIT.ToString() },
                {"MaxConcurrentRequestsOptions:LimitExceededPolicy", MaxConcurrentRequestsLimitExceededPolicy.FifoQueueDropHead.ToString() },
                {"MaxConcurrentRequestsOptions:MaxQueueLength", SOME_MAX_QUEUE_LENGTH.ToString() }
            };

            HttpResponseInformation[] responseInformation = GetResponseInformation(configuration, SOME_CONCURRENT_REQUESTS_COUNT);

            Assert.Equal(SOME_CONCURRENT_REQUESTS_COUNT - SOME_MAX_CONCURRENT_REQUESTS_LIMIT - SOME_MAX_QUEUE_LENGTH, responseInformation.Count(i => i.StatusCode == HttpStatusCode.ServiceUnavailable));
        }

        [Fact]
        public void SomeMaxConcurrentRequestsLimit_Queue_SomeMaxQueueLength_MaxTimeInQueueShorterThanProcessing_SomeConcurrentRequestsCount_CountMinusLimitRequestsReturnServiceUnavailable()
        {
            Dictionary<string, string> configuration = new Dictionary<string, string>
            {
                {"MaxConcurrentRequestsOptions:Limit", SOME_MAX_CONCURRENT_REQUESTS_LIMIT.ToString() },
                {"MaxConcurrentRequestsOptions:LimitExceededPolicy", MaxConcurrentRequestsLimitExceededPolicy.FifoQueueDropTail.ToString() },
                {"MaxConcurrentRequestsOptions:MaxQueueLength", SOME_MAX_QUEUE_LENGTH.ToString() },
                {"MaxConcurrentRequestsOptions:MaxTimeInQueue", TIME_SHORTER_THAN_PROCESSING.ToString() }
            };

            HttpResponseInformation[] responseInformation = GetResponseInformation(configuration, SOME_CONCURRENT_REQUESTS_COUNT);

            Assert.Equal(SOME_CONCURRENT_REQUESTS_COUNT - SOME_MAX_CONCURRENT_REQUESTS_LIMIT, responseInformation.Count(i => i.StatusCode == HttpStatusCode.ServiceUnavailable));
        }
        #endregion

        #region Methods
        private HttpResponseInformation[] GetResponseInformation(Dictionary<string, string> configuration, int concurrentRequestsCount)
        {
            HttpResponseInformation[] responseInformation;

            using (TestServer server = PrepareTestServer(configuration))
            {
                List<HttpClient> clients = new List<HttpClient>();
                for (int i = 0; i < concurrentRequestsCount; i++)
                {
                    clients.Add(server.CreateClient());
                }

                List<Task<HttpResponseMessageWithTiming>> responsesWithTimingsTasks = new List<Task<HttpResponseMessageWithTiming>>();
                foreach (HttpClient client in clients)
                {
                    responsesWithTimingsTasks.Add(Task.Run(async () => { return await client.GetWithTimingAsync("/"); }));
                }
                Task.WaitAll(responsesWithTimingsTasks.ToArray());

                clients.ForEach(client => client.Dispose());

                responseInformation = responsesWithTimingsTasks.Select(task => new HttpResponseInformation
                {
                    StatusCode = task.Result.Response.StatusCode,
                    Timing = task.Result.Timing
                }).ToArray();
            }

            return responseInformation;
        }
        #endregion
    }
}
