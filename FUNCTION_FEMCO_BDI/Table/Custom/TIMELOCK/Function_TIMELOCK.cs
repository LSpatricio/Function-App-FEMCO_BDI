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


namespace FUNCTION_FEMCO_BDI.Table.Custom.TIMELOCK
{
    public class Function_TIMELOCK
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.TIMELOCK";

        public Function_TIMELOCK(ILoggerFactory loggerFactory,DAO_SQL dao,ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_TIMELOCK>();
            _dao = dao;
            _icmservice = icmService;
        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_TIMELOCK()
        {

            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "TimeLock";

            string ConsultaICM = @"SELECT TimeID,
                                           Period,
                                           ComputedAllAt
                                            FROM " + TablaICM;

            string parametros = "";

            DataTable dt = new DataTable();
            dt.Columns.Add("TimeID", typeof(string));
            dt.Columns.Add("Period", typeof(string));
            dt.Columns.Add("ComputedAllAt", typeof(decimal));

            dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, dt, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;


        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_TIMELOCK")]
        public async Task<HttpResponseData> BulkCreate_Trigger_TIMELOCK([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_TIMELOCK")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_TIMELOCK.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_TIMELOCK();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_TIMELOCK");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Diario dos ejecuciones. A las 8:30 am y 3:30 pm

        //[Function("BulkCreate_Timer_TIMELOCK")]
        //public async Task BulkCreate_Timer_TIMELOCK([TimerTrigger("0 30 8,15 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_TIMELOCK.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_TIMELOCK();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_TIMELOCK: {Message}", ex.Message);


        //    }


        //}
        //#endregion
        //[Function("BulkCreate_Timer_TIMELOCKPrueba")]
        //public async Task BulkCreate_Timer_TIMELOCKPrueba([TimerTrigger("0 30 8,15 * * 5")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_TIMELOCKPrueba.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_TIMELOCK();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_TIMELOCKPrueba: {Message}", ex.Message);


        //    }


        //}


      

    }
}
