using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Cryptography;
using System.Text;
using System.Web.Hosting;

using log4net.Appender;
using log4net.Core;

namespace DataDogLog4NetAppender
{
    public class DatadogAppender : AppenderSkeleton
    {
        private const string PRIORITYLEVEL_NORMAL = "normal";
        private const string PRIORITYLEVEL_LOW = "low";
        private const string ALERTLEVEL_SUCCESS = "success";
        private const string ALERTLEVEL_INFO = "info";
        private const string ALERTLEVEL_WARN = "warn";
        private const string ALERTLEVEL_ERROR = "error";
        public string ApiKey { get; set; }
        public string Tags { get; set; }

        IEnumerable<string> EnvironmentalTags
        {
            get
            {
                if(HostingEnvironment.ApplicationHost != null)
                {
                    var siteName = HostingEnvironment.ApplicationHost.GetSiteName();
                    if(siteName != null)
                    {
                        yield return "iis_site_" + siteName.Replace(" ", "_").ToLower();
                    }
                }

                yield return "host_" + Environment.MachineName.Replace(" ", "_").ToLower();
            }
        }

        private readonly HttpClient _client;

        public DatadogAppender()
        {
            _client = new HttpClient {BaseAddress = new Uri("https://app.datadoghq.com/api/")};
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if(string.IsNullOrEmpty(ApiKey))
                return;

            var tags = (Tags ?? string.Empty)
                .Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)
                .Concat(EnvironmentalTags)
                .OrderBy(x => x)
                .Distinct()
                .ToList();

            string text = RenderLoggingEvent(loggingEvent);

            string title = loggingEvent.MessageObject?.ToString() ?? (loggingEvent.ExceptionObject?.Message ?? text);

            if (title.Length > 100)
                title = title.Substring(0, 100);


            string priority = loggingEvent.Level >= Level.Error ? PRIORITYLEVEL_NORMAL : PRIORITYLEVEL_LOW;

            string alertType = GetAlertType(loggingEvent);

            string aggregateKey = GetAggregationKey(title, loggingEvent.LoggerName);

            var datadogEvent = new DatadogEvent
                               {
                                   Tags = tags,
                                   AlertType = alertType,
                                   Priority = priority,
                                   Text = text,
                                   Title = title,
                                   AggregationKey = aggregateKey
                               };

            var content = new ObjectContent(typeof(DatadogEvent),
                datadogEvent,
                new JsonMediaTypeFormatter(),
                "application/json");

            _client.PostAsync($"v1/events?api_key={ApiKey}", content).Result.EnsureSuccessStatusCode();
        }

        private string GetAlertType(LoggingEvent loggingEvent)
        {
            if(loggingEvent.Level == Level.Debug || loggingEvent.Level == Level.Info)
                return ALERTLEVEL_INFO;

            if(loggingEvent.Level == Level.Warn)
                return ALERTLEVEL_WARN;

            return loggingEvent.ExceptionObject == null ? ALERTLEVEL_SUCCESS : ALERTLEVEL_ERROR;
        }

        private string GetAggregationKey(string message, string loggerName)
        {
            var composedMessage = $"{loggerName}.{message}";
            byte[] bytes = Encoding.Unicode.GetBytes(composedMessage);
            byte[] hash = new SHA256Managed().ComputeHash(bytes);
            string hashContents = Convert.ToBase64String(hash);

            int maxCharacters = Math.Min(message.Length, 100 - hashContents.Length);

            return message.Substring(0, maxCharacters) + hashContents;
        }
    }
}