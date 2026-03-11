using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.DTOs
{
    public class RunScheduleitemResponse
    {
        public string CompletedActivities { get; set; }
        public string LiveActivities { get; set; }

        //   public string GetRunId()
        // {
        //   return CompletedActivities?.Split('/').LastOrDefault();
        //}

        public string GetRunId() =>
            string.IsNullOrWhiteSpace(CompletedActivities)
                ? null
                : CompletedActivities.Split('/').LastOrDefault();
    }
}
