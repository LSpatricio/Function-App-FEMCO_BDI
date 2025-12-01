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
using FUNCTION_FEMCO_BDI.Funcionalidades;
using System.Globalization;

namespace FUNCTION_FEMCO_BDI.Table.Custom.TIMECALENDAR
{
    public class Function_TIMECALENDAR
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCO.TIMECALENDAR";

        public Function_TIMECALENDAR(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_TIMECALENDAR>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_TIMECALENDAR()
        {

            string modeloICM = Environment.GetEnvironmentVariable("ModelFemco");
            string TablaICM = "TimeCalendar";
            string ConsultaICM = @"SELECT TimeID,
                                            LastLockedPeriod
                                                 FROM " + TablaICM;

            string parametros = $@"";

            DataTable dt = new DataTable();
            dt.Columns.Add("TimeID", typeof(string));
            dt.Columns.Add("LastLockedPeriod", typeof(string));
  

            dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, dt, parametros);
            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;



        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_TIMECALENDAR")]
        public async Task<HttpResponseData> BulkCreate_Trigger_TIMECALENDAR([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_TIMECALENDAR")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_TIMECALENDAR.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");


            try
            {
                string mensaje = await BulkCreate_TIMECALENDAR();

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
                
                await response.WriteAsJsonAsync(new
                {
                    errorCode = "INTERNAL_ERROR",
                    message = "Ocurrió un error interno. Inténtalo más tarde.",
                });

            }
            finally
            {
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_TIMECALENDAR");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////1 vez al día, del 1 al 7 del mes, 5 pm

        //[Function("BulkCreate_Timer_TIMECALENDAR")]
        //public async Task BulkCreate_Timer_TIMECALENDAR([TimerTrigger("0 0 17 1-7 * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_TIMECALENDAR.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_TIMECALENDAR();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_TIMECALENDAR: {Message}", ex.Message);


        //    }


        //}
        //#endregion


    }
}
