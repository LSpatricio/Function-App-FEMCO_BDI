using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace FUNCTION_FEMCO_BDI.DTOs
{
    public class LiveActivitiesResponse
    {
        public Int32 ProgressId { set; get; }

        public string UserId { set; get; }

        public string Type { set; get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ActivityStatus? Status { set; get; }

        public DateTime Time { set; get; }

        public string ApiServer { set; get; }

        public Int32 Percent { set; get; }

        public string Description { set; get; }

        public bool HasDescription { set; get; }

        public DateTime ExpiresAt { set; get; }

        public bool IsCancellable { set; get; }

        public bool IsInitialization { set; get; }

        public Int32 ComputationId { set; get; }

        public bool IsRunning => Status == ActivityStatus.Running;


    }

    public enum ActivityStatus
    {
       
        Running,
        SinRespuesta
    }

}
