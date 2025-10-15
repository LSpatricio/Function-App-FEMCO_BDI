using FUNCTION_FEMCO_BDI.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;


namespace FUNCTION_FEMCO_BDI.Table.Custom.SCHEDULE
{
    public class Function_SCHEDULE
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.SCHEDULE";

        public Function_SCHEDULE(ILoggerFactory loggerFactory,DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_SCHEDULE>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_SCHEDULE()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEPDev");
            string TablaICM = "Schedule";

            string ConsultaICM = @"SELECT ScheduleID,
                                                ScheduleItemID,
                                                Minute,
                                                Hour,
                                                DayOfMonth,
                                                Month,
                                                DayOfWeek
                                                 FROM " + TablaICM;

            string parametros = "";

            DataTable dt = new DataTable();
            dt.Columns.Add("ScheduleID", typeof(decimal));
            dt.Columns.Add("ScheduleItemID", typeof(decimal));
            dt.Columns.Add("Minute", typeof(decimal));
            dt.Columns.Add("Hour", typeof(decimal));
            dt.Columns.Add("DayOfMonth", typeof(decimal));
            dt.Columns.Add("Month", typeof(decimal));
            dt.Columns.Add("DayOfWeek", typeof(decimal));

            dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, dt, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;

        }
        #endregion


        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_SCHEDULE")]
        public async Task<HttpResponseData> BulkCreate_Trigger_SCHEDULE([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_SCHEDULE")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_SCHEDULE.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_SCHEDULE();

                var result = new
                {
                    message = mensaje,
                    timestamp = DateTime.UtcNow
                };

                if (mensaje.Contains("Sin datos por insertar"))
                {
                    response.StatusCode = HttpStatusCode.Accepted; // 202 Accepted
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK; // 200 OK
                }

                await response.WriteStringAsync(JsonConvert.SerializeObject(result));
                _logger.LogInformation(mensaje);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al procesar la solicitud: {Message}", ex.Message);
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(ex.Message);

            }
            finally
            {
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_SCHEDULE");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Diario dos ejecuciones. A las 8:30 am y 3:30 pm

        //[Function("BulkCreate_Timer_SCHEDULE")]
        //public async Task BulkCreate_Timer_SCHEDULE([TimerTrigger("0 30 8,15 * * 1-5")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_SCHEDULE.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_SCHEDULE();
        //        _logger.LogInformation(mensaje);

        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_SCHEDULE: {Message}", ex.Message);


        //    }


        //}
        //#endregion



    }
}
