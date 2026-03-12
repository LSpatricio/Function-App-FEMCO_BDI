using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace FUNCTION_FEMCO_BDI.DTOs
{
    public class CompletedActivitiesResponse
    {
        public Int32 ProgressId { set; get; }

        public string UserId { set; get; }
        public string Type { set; get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CompletedActivityStatus? Status { set; get; }

        public string Message { set; get; }

        public DateTime Time { set; get; }

        public string ApiServer { set; get; }

        public bool IsCompleted => Status == CompletedActivityStatus.Completed;

    }

    public enum CompletedActivityStatus
    {
        Completed,
        Running,
        Failed,
        Cancelled,
        SinRespuesta
    }
}
