using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace acr_globalwebhook
{
    public static class AcrWebHook
    {
        private static string[] GetRegions()
        {
            // currently pulling from config, but could query ARM or ...
            return Environment.GetEnvironmentVariable("WebhookRegions") // TODO add error handling!
                            .Split(',')
                            .Select(s => s.Trim())
                            .ToArray();
        }

        private static string GetEventName(string region) => $"pushevent-{region}";
        private static TimeSpan GetTimeOutInterval()
        {
            var config = Environment.GetEnvironmentVariable("WebhookTimeOut"); // TODO add error handling!
            return TimeSpan.Parse(config);
        }


        // TODO - switch to AuthorizationLeve.Function!!

        [FunctionName("ImagePush")]
        public static async Task<HttpResponseMessage> ImagePush(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]
            HttpRequestMessage request,
            [OrchestrationClient]
            DurableOrchestrationClient starter,
            TraceWriter log)
        {
            var query = request.RequestUri.ParseQueryString();
            var region = query["region"];
            if (string.IsNullOrEmpty(region))
            {
                log.Error("*** TRIGGER: region querystring value missing");
                return request.CreateResponse(HttpStatusCode.BadRequest, "region querystring value missing", new JsonMediaTypeFormatter());
            }
            if (!GetRegions().Any(r => r.ToLowerInvariant() == region.ToLowerInvariant()))
            {
                log.Error("*** TRIGGER: region querystring value is not in the configured regions for the function");
                return request.CreateResponse(HttpStatusCode.BadRequest, "region querystring value is not in the configured regions for the function", new JsonMediaTypeFormatter());
            }

            var notification = await request.Content.ReadAsAsync<WebHookNotification>();
            var instanceId = notification.Id;
            log.Info($"got id {instanceId}");

            // Find or start an orchestration instance
            log.Info($"*** TRIGGER: Looking up instance: {instanceId}");
            var status = await starter.GetStatusAsync(instanceId);
            if (status == null)
            {
                log.Info($"*** TRIGGER: no instance found - {instanceId} - starting...");
                await starter.StartNewAsync("WaitForAllRegions", instanceId, notification);
                log.Info($"*** TRIGGER: Started orchestration with ID = '{instanceId}'.");
            }
            else
            {
                log.Info($"*** TRIGGER: Got existing instance for {instanceId} (name {status.Name}). status {status.RuntimeStatus})");
            }

            // Raise event for region in current notification
            await starter.RaiseEventAsync(instanceId, GetEventName(region), null);

            return starter.CreateCheckStatusResponse(request, instanceId);
        }

        [FunctionName("WaitForAllRegions")]
        public static async Task WaitForAllRegions(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var notification = context.GetInput<WebHookNotification>();

            // Set up a timeout to fire a notification if we don't get notifications from all regions in a timely manner
            var cts = new CancellationTokenSource();
            TimeSpan timeOutInterval = GetTimeOutInterval();
            Task timeoutTask = context.CreateTimer(context.CurrentUtcDateTime + timeOutInterval, cts.Token);

            // Wait for external event notifications from each region
            var eventTasks = GetRegions()
                                .Select(region => context.WaitForExternalEvent<object>(GetEventName(region)));
            Task replicationCompletedTask = Task.WhenAll(eventTasks);

            // wait until tasks complete or we time-out...
            var winningTask = await Task.WhenAny(timeoutTask, replicationCompletedTask);

            if (winningTask == timeoutTask)
            {
                // Fire the teimout notification
                await context.CallActivityAsync<string>("FireTimeoutNotification", notification);
            }
            else
            {
                // cancel the timer
                cts.Cancel();

                // Fire the replication notification
                await context.CallActivityAsync<string>("FirePushNotification", notification);
            }
        }


        [FunctionName("FirePushNotification")]
        public static string FirePushNotification([ActivityTrigger] WebHookNotification notification, TraceWriter log)
        {
            log.Info($"TODO: All regions are sync'd - add whatever onward notification you want here! Repository: {notification.Target.Repository}, Tag: {notification.Target.Tag}, Id: {notification.Request.Id}");
            return $"All regions are sync'd!";
        }
        [FunctionName("FireTimeoutNotification")]
        public static string FireTimeoutNotification([ActivityTrigger] WebHookNotification notification, TraceWriter log)
        {
            log.Info($"TODO: Timed out waiting for replication - add whatever onward notification you want here! Repository: {notification.Target.Repository}, Tag: {notification.Target.Tag}, Id: {notification.Request.Id}");
            return $"Timed out";
        }


        public class WebHookNotification
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("timestamp")]
            public string Timestamp { get; set; }
            [JsonProperty("action")]
            public string Action { get; set; }
            [JsonProperty("target")]
            public Target Target { get; set; }
            [JsonProperty("request")]
            public Request Request { get; set; }
        }

        public class Target
        {
            [JsonProperty("mediaType")]
            public string MediaType { get; set; }
            [JsonProperty("size")]
            public int Size { get; set; }
            [JsonProperty("digest")]
            public string Digest { get; set; }
            [JsonProperty("length")]
            public int Length { get; set; }
            [JsonProperty("repository")]
            public string Repository { get; set; }
            [JsonProperty("tag")]
            public string Tag { get; set; }
        }

        public class Request
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("host")]
            public string Host { get; set; }
            [JsonProperty("method")]
            public string Method { get; set; }
            [JsonProperty("useragent")]
            public string UserAgent { get; set; }
        }
    }
}
