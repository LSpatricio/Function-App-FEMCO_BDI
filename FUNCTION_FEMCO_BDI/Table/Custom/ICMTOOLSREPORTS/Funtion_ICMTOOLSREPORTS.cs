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

namespace FUNCTION_FEMCO_BDI.Table.Custom.ICMTOOLSREPORTS
{
    public class Function_ICMTOOLSREPORTS
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCO_Transfer.ICMTOOLSREPORTS";

        public Function_ICMTOOLSREPORTS(ILoggerFactory loggerFactory,DAO_SQL dao, ICMService icmService)
        {
            _dao = dao;
            _logger = loggerFactory.CreateLogger<Function_ICMTOOLSREPORTS>();
            _icmservice = icmService;
        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_ICMTOOLSREPORTS()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemco");
            string TablaICM = "FemcoTransferICMToolsReports";

            string ConsultaICM = @"SELECT ReportId,
                                                Name,
                                                Subject,
                                                Body,
                                                Footer,
                                                To,
                                                Cc,
                                                Bcc,
                                                CreatedDate
                                                 FROM " + TablaICM;

            string parametros = "";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;

        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_ICMTOOLSREPORTS")]
        public async Task<HttpResponseData> BulkCreate_Trigger_ICMTOOLSREPORTS([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_ICMTOOLSREPORTS")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_ICMTOOLSREPORTS.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_ICMTOOLSREPORTS();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_ICMTOOLSREPORTS");

            }

            return response;

        }
        #endregion


        //#region BulkCreate como Azure Function Timer 

        ////Todos los dias 4:30am
        //[Function("BulkCreate_Timer_ICMTOOLSREPORTS_DailyTask")]
        //public async Task BulkCreate_Timer_ICMTOOLSREPORTS_DailyTask([TimerTrigger("0 30 4 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_ICMTOOLSREPORTS_DailyTask.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_ICMTOOLSREPORTS();
        //        _logger.LogInformation(mensaje);

        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_ICMTOOLSREPORTS_DailyTask: {Message}", ex.Message);


        //    }


        //}


        ////Lunes y Viernes 7:30am hasta las 11:30 pm, cada hora. 
        //[Function("BulkCreate_Timer_ICMTOOLSREPORTS_MondayFridayTask")]
        //public async Task BulkCreate_Timer_ICMTOOLSREPORTS_MondayFridayTask([TimerTrigger("0 30 7-23 * * 1,5")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_ICMTOOLSREPORTS_MondayFridayTask.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_ICMTOOLSREPORTS();
        //        _logger.LogInformation(mensaje);

        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_ICMTOOLSREPORTS_MondayFridayTask: {Message}", ex.Message);


        //    }


        //}
        //#endregion


    }
}
