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
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FUNCTION_FEMCO_BDI.Table.Custom.SCHEDULE;

namespace FUNCTION_FEMCO_BDI.Table.Custom.SCHEDULEITEM
{
    public class Function_SCHEDULEITEM
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.SCHEDULEITEM";

        public Function_SCHEDULEITEM(ILoggerFactory loggerFactory,DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_SCHEDULEITEM>();
            _dao = dao;
            _icmservice = icmService;
        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_SCHEDULEITEM()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEPDev");
            string TablaICM = "ScheduleItem";

            string ConsultaICM = @"SELECT ScheduleItemID,
                                                Name,
                                                Parent,
                                                Type,
                                                Order,
                                                LastRun,
                                                LastRunStatus,
                                                Activation
                                                
                                                 FROM " + TablaICM;

            string parametros = "";

            DataTable dt = new DataTable();
            dt.Columns.Add("ScheduleItemID", typeof(decimal));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Parent", typeof(decimal));
            dt.Columns.Add("Type", typeof(decimal));
            dt.Columns.Add("Order", typeof(decimal));
            dt.Columns.Add("LastRun", typeof(DateTime));
            dt.Columns.Add("LastRunStatus", typeof(decimal));
            dt.Columns.Add("Activation", typeof(decimal));

            dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, dt, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;

        }
        #endregion


        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_SCHEDULEITEM")]
        public async Task<HttpResponseData> BulkCreate_Trigger_SCHEDULEITEM([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_SCHEDULEITEM")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_SCHEDULEITEM.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_SCHEDULEITEM();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_SCHEDULEITEM");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Diario dos ejecuciones. A las 8:30 am y 3:30 pm

        //[Function("BulkCreate_Timer_SCHEDULEITEM")]
        //public async Task BulkCreate_Timer_SCHEDULEITEM([TimerTrigger("0 30 8,15 * * 1-5")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_SCHEDULEITEM.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_SCHEDULEITEM();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_SCHEDULEITEM: {Message}", ex.Message);


        //    }


        //}
        //#endregion



    }
}
