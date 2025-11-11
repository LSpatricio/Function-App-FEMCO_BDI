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


namespace FUNCTION_FEMCO_BDI.Table.Custom.SPTASIGNACIONCENTROTRABAJO
{
    public class Function_SPTASIGNACIONCENTROTRABAJO
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCO.SPTASIGNACIONCENTROTRABAJO";

        public Function_SPTASIGNACIONCENTROTRABAJO(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_SPTASIGNACIONCENTROTRABAJO>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_SPTASIGNACIONCENTROTRABAJO()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemco");
            string TablaICM = "sptAsignacionCentroTrabajo";

            string ConsultaICM = @"SELECT CentroTrabajoID,
                            EmpleadoID,
                            RolID,
                            FechaInicio,
                            FechaFin,
                            FuncionID
                            FROM " + TablaICM;

            string parametros = "";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;
        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_SPTASIGNACIONCENTROTRABAJO")]
        public async Task<HttpResponseData> BulkCreate_Trigger_SPTASIGNACIONCENTROTRABAJO([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_SPTASIGNACIONCENTROTRABAJO")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_SPTASIGNACIONCENTROTRABAJO.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");


            try
            {
                string mensaje = await BulkCreate_SPTASIGNACIONCENTROTRABAJO();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_SPTASIGNACIONCENTROTRABAJO");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////3 veces por dia, 6, 9 y 2 pm, 

        //[Function("BulkCreate_Timer_SPTASIGNACIONCENTROTRABAJO")]
        //public async Task BulkCreate_Timer_SPTASIGNACIONCENTROTRABAJO([TimerTrigger("0 0 6,9,14 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_SPTASIGNACIONCENTROTRABAJO.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_SPTASIGNACIONCENTROTRABAJO();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_SPTASIGNACIONCENTROTRABAJO: {Message}", ex.Message);


        //    }


        //}
        //#endregion




    }
}
