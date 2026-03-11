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

            string modeloFemco = Environment.GetEnvironmentVariable("ModeloFemco");

            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            RunScheduleitemResponse runScheduleitemResponse = await _icmservice.EjecutarScheduleitem("4638", modeloFemco);   

            string runid = runScheduleitemResponse.GetRunId();

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
