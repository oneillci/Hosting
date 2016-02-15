﻿using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hosting.Tests
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            string baseAddress = args.Length > 0 && !string.IsNullOrEmpty(args[0]) ? args[0] : "http://localhost:8000";

            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    cts.Cancel();

                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                RunTestsRepeatedlyAsync(new Uri(baseAddress), cts.Token).GetAwaiter().GetResult();
            }
        }

        private static async Task RunTestsRepeatedlyAsync(Uri baseAddress, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient(new ConsoleLoggingHandler(), true))
            {
                client.BaseAddress = baseAddress;

                int iteration = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    await RunTestsAsync(client, cancellationToken);

                    Console.WriteLine($"Iteration {++iteration} completed.");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        private static async Task RunTestsAsync(HttpClient client, CancellationToken cancellationToken)
        {
            try
            {
                // SMS
                await client.PostAsync("/sms/api/sms/unicorn/hello", new StringContent(string.Empty), cancellationToken);

                await client.GetAsync("/sms/api/sms/unicorn", cancellationToken);

                // Counter
                await client.PostAsync("/counter/api/counter", new StringContent(string.Empty), cancellationToken);

                await client.GetAsync("/counter/api/counter", cancellationToken);

                await client.GetAsync("/Hosting/CounterService/api/counter", cancellationToken);

                var request = new HttpRequestMessage(HttpMethod.Get, "/api/counter");
                request.Headers.Add("SF-ServiceName", "fabric:/Hosting/CounterService");
                await client.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private sealed class ConsoleLoggingHandler : DelegatingHandler
        {
            public ConsoleLoggingHandler()
            {
                InnerHandler = new HttpClientHandler();
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var stopWatch = Stopwatch.StartNew();

                var response = await base.SendAsync(request, cancellationToken);

                stopWatch.Stop();

                Console.WriteLine($"Status: {response.StatusCode} Method: {response.RequestMessage.Method} URL: {response.RequestMessage.RequestUri} Time elapsed: {stopWatch.Elapsed}");

                return response;
            }
        }
    }
}
