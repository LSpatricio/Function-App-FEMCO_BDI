using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.Table.Custom.DATESTRINGPERIODS.Classes
{
    public class CL_DATESTRINGPERIODS
{
    public string PeriodString { get; set; }

    public string PeriodName { get; set; }

    public DateTime? StarDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string QuaterString { get; set; }

    public decimal NumOfWeeks { get; set; }

    public decimal NumOfDays { get; set; }

    public decimal MonthInYear { get; set; }

    public string MonthInYearString { get; set; }

    public string MonthInYearString0 { get; set; }

    public string MonthInQtr { get; set; }

    public string MonthInQtrString { get; set; }

    public string MonthName { get; set; }

    public string MonthNameAbbr { get; set; }

    public decimal PeriodIncrement { get; set; }

    public string PriorPeriod { get; set; }

    public string NextPeriod { get; set; }

    public string PriorYearPeriod { get; set; }

    public string NextYearPeriod { get; set; }

    public string ReportDisplayLabel { get; set; }

    public string IsReportDisplay { get; set; }

    public DateTime EffStart_ { get; set; }

    public DateTime EffEnd_ { get; set; }

    public string IsOutputInterface { get; set; }

}
}
