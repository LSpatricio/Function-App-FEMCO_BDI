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


namespace FUNCTION_FEMCO_BDI.Table.Custom.SYSFECHAHOY
{
    public class Function_SYSFECHAHOY
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCO.SYSFECHAHOY";

        public Function_SYSFECHAHOY(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_SYSFECHAHOY>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_SYSFECHAHOY()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemco");
            string TablaICM = "sysFechaHoy";

            string ConsultaICM = @"SELECT ID,
                            Fecha,
                            FechaDate
                            FROM " + TablaICM;

            string parametros = "";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;
        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_SYSFECHAHOY")]
        public async Task<HttpResponseData> BulkCreate_Trigger_SYSFECHAHOY([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_SYSFECHAHOY")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_SYSFECHAHOY.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_SYSFECHAHOY();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_SYSFECHAHOY");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////3 veces al día, todos los días

        //[Function("BulkCreate_Timer_SYSFECHAHOY")]
        //public async Task BulkCreate_Timer_SYSFECHAHOY([TimerTrigger("0 0 */8 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_SYSFECHAHOY.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_SYSFECHAHOY();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_SYSFECHAHOY: {Message}", ex.Message);


        //    }


        //}
        //#endregion




    }
}
