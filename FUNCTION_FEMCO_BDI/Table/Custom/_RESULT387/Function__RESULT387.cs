using Azure;
using FUNCTION_FEMCO_BDI.DAO;
using FUNCTION_FEMCO_BDI.DTOs;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Apis.Requests.BatchRequest;


namespace FUNCTION_FEMCO_BDI.Table.Custom._RESULT387
{
    public class Function__RESULT387
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP._RESULT387";

        public Function__RESULT387(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function__RESULT387>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate__RESULT387()
        {


            DataTable dtfechas = FuncionalidadICM.getdates(3);
            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];
            DateTime dateEnd= (DateTime)dtfechas.Rows[0]["DateEnd"];

            DateTime lastDayOfMonth = new DateTime(dateEnd.Year, dateEnd.Month, DateTime.DaysInMonth(dateEnd.Year, dateEnd.Month));
            // Formato MM/dd/yyyystring
            string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            string dateEndFormatted = lastDayOfMonth.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "_Result387";

            RunScheduleitemResponse runScheduleitemResponse = await _icmservice.EjecutarSincronizacion("387", modeloICM);
            string runId = runScheduleitemResponse.GetRunId();

            if (string.IsNullOrEmpty(runId))
            {
                throw new Exception("No se pudo obtener el RunId de la importación.");
            }

            LiveActivitiesResponse liveActivitiesResponse;

            do
            {
                _logger.LogInformation("Proceso con RunId: " + runId + " ejecutandose.");
                liveActivitiesResponse = await _icmservice.ConsultarLiveActivitie(runId, modeloICM);
                if (liveActivitiesResponse == null)
                {
                    liveActivitiesResponse = new LiveActivitiesResponse
                    {
                        Status = ActivityStatus.SinRespuesta
                    };


                }

                await Task.Delay(5000);
            }
            while (liveActivitiesResponse.IsRunning);


            CompletedActivitiesResponse completedActiviesResponse = await _icmservice.ConsultarCompletedActivitie(runId, modeloICM); 

            if (completedActiviesResponse == null)
            {
                completedActiviesResponse = new CompletedActivitiesResponse
                {
                    Status = CompletedActivityStatus.Completed
                };
            }
            if (completedActiviesResponse.IsCompleted)
            {
                _logger.LogInformation($"{completedActiviesResponse.Status}");
            }
            else
            {

                if (completedActiviesResponse.Status == CompletedActivityStatus.Cancelled)
                {
                    throw new HttpRequestException($"Sincronizacion cancelada");
                }
                else
                {
                    throw new HttpRequestException($"Error en sincronizacion");
                }
            }
                


           
            List<string> columnas = new List<string>
            {
                "_ResultID",
                "IDSociety",
                "IDPersonalDivision",
                "IDStore",
                "PayeeID_",
                "IDRole",
                "IDCalculation",
                "Weeks",
                "Value"
            };
            string parametros = $@" A INNER JOIN \""CfgDateStringPeriod\"" B ON A.\""Weeks\"" =  B.\""PeriodName\"" WHERE \""DateStart\"" BETWEEN '{dateStartFormatted}' AND '{dateEndFormatted}'";
            //string orderBy = @" ORDER BY  \""IDStore\"", \""PayeeID_\"", \""IDRole\"", \""DateString\"", \""Weeks\"" ";
            string mensaje = "";

            string columnasFormateadas = FuncionalidadICM.FormatearColumnas(columnas);
            string orderBy = $@" ORDER BY  {columnasFormateadas}";


            string countConsulta = FuncionalidadICM.ConsultaAjustada(TablaICM, parametros);

            string consultaICM = FuncionalidadICM.ConsultaAjustada(TablaICM, parametros, columnasFormateadas);

            DataTable dtCount = await _icmservice.ConsultaICMQuerytool(TablaICM, countConsulta, modeloICM, 0);

            int count = int.Parse(dtCount.Rows[0][0].ToString());

            if (count == 0)
            {
                return "Sin datos por insertar en la tabla " + NOMBRE_TABLA;
            }

            await _dao.TruncateTable(NOMBRE_TABLA);

            for (int i = 0; i < count; i += 400000)
            {
                DataTable dtParte = await _icmservice.ConsultaICMQuerytool(TablaICM, $"{consultaICM} {orderBy}", modeloICM, i);
                mensaje = await _dao.bulkInsert(dtParte, NOMBRE_TABLA);
            }


            return mensaje;



        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger__RESULT387")]
        public async Task<HttpResponseData> BulkCreate_Trigger__RESULT387([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger__RESULT387")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                _logger.LogInformation("Inicio de la función BulkCreate_Trigger__RESULT387.");
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string mensaje = await BulkCreate__RESULT387();

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
                await response.WriteAsJsonAsync(new
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    errorCode = "INTERNAL_ERROR",
                    message = "Ocurrió un error interno. Inténtalo más tarde.",
                });
                response.StatusCode = HttpStatusCode.InternalServerError;

            }
            finally
            {
                _logger.LogInformation("Fin de la función BulkCreate_Trigger__RESULT387");

            }

            return response;

        }
        #endregion

        #region BulkCreate como Azure Function Timer.

        //Jueves-Sabado 10:30 am
        [Function("BulkCreate_Timer__RESULT387")]
        public async Task BulkCreate_Timer__RESULT387([TimerTrigger("0 30 10 * * 4,6")] TimerInfo myTimer)
        {

            _logger.LogInformation("Inicio de la función BulkCreate_Timer__RESULT387.");

            try
            {
                string mensaje = await BulkCreate__RESULT387();
                _logger.LogInformation(mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer__RESULT387: {Message}", ex.Message);
            }
            finally
            {
                _logger.LogInformation("Fin de la función BulkCreate_Timer__RESULT387.");
            }
        }

     
        #endregion

    }
}
