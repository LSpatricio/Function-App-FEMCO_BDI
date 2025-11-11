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
using System.Globalization;
using FUNCTION_FEMCO_BDI.Funcionalidades;


namespace FUNCTION_FEMCO_BDI.Table.Custom.DTCIERREVIRTUAL
{
    public class Function_DTCIERREVIRTUAL
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCO.DTCIERREVIRTUAL";

        public Function_DTCIERREVIRTUAL(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_DTCIERREVIRTUAL>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_DTCIERREVIRTUAL()
        {
            DataTable dtfechas = FuncionalidadICM.getdates();

            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

            string dateStartFormatted = dateStart.ToString("yyyy MMM", new CultureInfo("es-MX")).ToUpper();

            dateStartFormatted = dateStartFormatted.Remove(dateStartFormatted.Length - 1);

            dateStartFormatted = dateStartFormatted.Replace(" ", "%");


            string modeloICM = Environment.GetEnvironmentVariable("ModelFemco");
            string TablaICM = "dtCierreVirtual";

            string ConsultaICM = @"SELECT CentroTrabajoID,
                            Periodo,
                            FechaBloqueo,
                            FechaCancelacion,
                            Cerrado,
                            BloqueadorID,
                            CanceladorID
                            FROM " + TablaICM;

            string parametros = $@" WHERE \""Periodo\"" ILIKE '{dateStartFormatted}'";

            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;
        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_DTCIERREVIRTUAL")]
        public async Task<HttpResponseData> BulkCreate_Trigger_DTCIERREVIRTUAL([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_DTCIERREVIRTUAL")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_DTCIERREVIRTUAL.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");


            try
            {
                string mensaje = await BulkCreate_DTCIERREVIRTUAL();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_DTCIERREVIRTUAL");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Del 1 al 4 del mes, cada 20 min

        //[Function("BulkCreate_Timer_DTCIERREVIRTUAL")]
        //public async Task BulkCreate_Timer_DTCIERREVIRTUAL([TimerTrigger("0 20,50 * 1-4 * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_DTCIERREVIRTUAL.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_DTCIERREVIRTUAL();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_DTCIERREVIRTUAL: {Message}", ex.Message);


        //    }


        //}
        //#endregion




    }
}
