using FUNCTION_FEMCO_BDI.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using System.Globalization;

namespace FUNCTION_FEMCO_BDI.Table.Custom.HISTORYREVISION
{
    public class Function_HISTORYREVISION
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.HISTORYREVISION";

        public Function_HISTORYREVISION(ILoggerFactory loggerFactory,DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_HISTORYREVISION>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_HISTORYREVISION()
        {
            DataTable dtfechas = FuncionalidadICM.getdates();

            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

            // Formato MM/dd/yyyystring
            string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "HistoryRevision";

            string ConsultaICM = @"SELECT RevisionID,
                                AdminID,
                                StartedAt
                            FROM " + TablaICM;

            string parametros = $@" WHERE \""StartedAt\"" >= '{dateStartFormatted}' ";

            // Crear un DataTable manualmente
            DataTable dt = new DataTable();         
            dt.Columns.Add("RevisionID", typeof(decimal));         
            dt.Columns.Add("AdminID", typeof(string)); 
            dt.Columns.Add("StartedAt", typeof(DateTime));

            dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, dt, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;

        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_HISTORYREVISION")]
        public async Task<HttpResponseData> BulkCreate_Trigger_HISTORYREVISION([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_HISTORYREVISION")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                _logger.LogInformation("Inicio de la función BulkCreate_Trigger_HISTORYREVISION.");
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string mensaje = await BulkCreate_HISTORYREVISION();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_HISTORYREVISION");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Diario dos ejecuciones. A las 8:30 am y 3:30 pm

        //[Function("BulkCreate_Timer_HISTORYREVISION")]
        //public async Task BulkCreate_Timer_HISTORYREVISION([TimerTrigger("0 30 8,15 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_HISTORYREVISION.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_HISTORYREVISION();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_HISTORYREVISION: {Message}", ex.Message);


        //    }


        //}
        //#endregion




    }
}
