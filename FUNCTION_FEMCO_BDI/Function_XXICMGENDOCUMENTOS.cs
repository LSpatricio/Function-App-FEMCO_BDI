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
            _logger.LogInformation("Inicio de la funcion Function_XXICMGENDOCUMENTOS");

            string modeloFemco = Environment.GetEnvironmentVariable("ModelFemco");

            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            _logger.LogInformation("Ejecutando scheduler");

            RunScheduleitemResponse runScheduleitemResponse = await _icmservice.EjecutarScheduleitem("4636", modeloFemco);   

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




            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
