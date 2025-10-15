using FUNCTION_FEMCO_BDI.DAO;
using System;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


namespace FUNCTION_FEMCO_BDI.Table.Custom.CATABSENCE
{
    public class Function_SYSULTIMAIMPORTACIONEXITOSA
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.CATABSENCE";

        public Function_SYSULTIMAIMPORTACIONEXITOSA(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_SYSULTIMAIMPORTACIONEXITOSA>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_CATABSENCE()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEPDev");
            string TablaICM = "CatAbsence";

            string ConsultaICM = @"SELECT IDAbsence,
                            Description,
                            Enable,
                            TransportationAid,
                            PerfectAttendanceFlag
                            FROM " + TablaICM;

            string parametros = "";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;
        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_CATABSENCE")]
        public async Task<HttpResponseData> BulkCreate_Trigger_CATABSENCE([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_CATABSENCE")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_CATABSENCE.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();


            try
            {
                string mensaje = await BulkCreate_CATABSENCE();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_CATABSENCE");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Diario dos ejecuciones. A las 8:30 am y 3:30 pm

        //[Function("BulkCreate_Timer_CATABSENCE")]
        //public async Task BulkCreate_Timer_CATABSENCE([TimerTrigger("0 30 8,15 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_CATABSENCE.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_CATABSENCE();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_CATABSENCE: {Message}", ex.Message);


        //    }


        //}
        //#endregion




    }
}
