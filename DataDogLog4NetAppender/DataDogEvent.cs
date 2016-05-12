using System.Collections.Generic;

using Newtonsoft.Json;

namespace DataDogLog4NetAppender
{
    public class DatadogEvent
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("priority")]
        public string Priority { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("alert_type")]
        public string AlertType { get; set; }

        [JsonProperty("aggregation_key")]
        public string AggregationKey { get; set; }
    }
}