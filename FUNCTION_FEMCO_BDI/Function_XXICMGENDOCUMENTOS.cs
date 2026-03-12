using Azure;
using FUNCTION_FEMCO_BDI.DAO;
using FUNCTION_FEMCO_BDI.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;

namespace FUNCTION_FEMCO_BDI
{
    public class Function_XXICMGENDOCUMENTOS
    {
        private readonly ILogger _logger;
        private readonly ICMService _icmservice;
        public Function_XXICMGENDOCUMENTOS(ILoggerFactory loggerFactory, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_XXICMGENDOCUMENTOS>();
            _icmservice = icmService;
        }

        [Function("Function_XXICMGENDOCUMENTOS")]
        public async Task<HttpResponseData> EjecutarProcesoGenDocumentos([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {

                _logger.LogInformation("Inicio de la funcion Function_XXICMGENDOCUMENTOS");

                var request = await req.ReadFromJsonAsync<ImportResquest>();

                if (request == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("El body de la solicitud es inválido.");
                    return response;
                }

                string modeloFemco = Environment.GetEnvironmentVariable("ModelFemco");

                response.Headers.Add("Content-Type", "application/json; charset=utf-8");

                _logger.LogInformation("Ejecutando scheduler");

                RunScheduleitemResponse runScheduleitemResponse = await _icmservice.EjecutarScheduleitem(request.scheduleItemId, modeloFemco);

                string runId = runScheduleitemResponse.GetRunId();

                if (string.IsNullOrEmpty(runId))
                {
                    throw new Exception("No se pudo obtener el RunId de la importación.");
                }

                LiveActivitiesResponse liveActivitiesResponse;

                do
                {
                    _logger.LogInformation("Importación con RunId: " + runId + " ejecutandose.");
                    liveActivitiesResponse = await _icmservice.ConsultarLiveActivitie(runId, modeloFemco);
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


                CompletedActivitiesResponse completedActiviesResponse = await _icmservice.ConsultarCompletedActivitie(runId, modeloFemco); ;

                if (completedActiviesResponse == null)
                {
                    completedActiviesResponse = new CompletedActivitiesResponse
                    {
                        Status = CompletedActivityStatus.Completed
                    };
                }

                string mensaje = "";

                if (completedActiviesResponse.IsCompleted)
                {
                    mensaje = "Proceso de generación de documentos completado exitosamente.";
                    response.StatusCode = HttpStatusCode.Accepted;
                }
                else
                {

                    mensaje = $"Proceso de generación de documentos finalizado con estado: {completedActiviesResponse.Status}.";

                    if (completedActiviesResponse.Status == CompletedActivityStatus.Cancelled)
                    {
                        response.StatusCode = HttpStatusCode.Gone;

                    }
                    else
                    {
                        response.StatusCode = HttpStatusCode.InternalServerError;
                    }


                }
                var result = new
                {
                    message = mensaje,
                    timestamp = DateTime.UtcNow
                };


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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_AUDIT_FEMCO");

            }

            return response;
        }
    }
}
