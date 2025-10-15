using FUNCTION_FEMCO_BDI.DAO;
using FUNCTION_FEMCO_BDI.Table.Ventas.LOGINTERFASES.Classes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.Table.Ventas.LOGINTERFASES
{
    public class Function_LOGINTERFASES
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private const string NOMBRE_TABLA = "LogInterfases";
        public Function_LOGINTERFASES(ILoggerFactory loggerFactory, DAO_SQL dao)
        {
            _logger = loggerFactory.CreateLogger<Function_LOGINTERFASES>();
            _dao = dao;

        }


        #region Select a tabla
        [Function("GetRows_LOGINTERFASES")]
        public async Task<HttpResponseData> GetAllRows_LOGINTERFASES(
            [HttpTrigger(AuthorizationLevel.Function, "get", "options", Route = "GetRows_LOGINTERFASES")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función GetAllRows_LOGINTERFASES");

            var response = req.CreateResponse();

            // Soporte para solicitud preflight (OPTIONS)
            if (req.Method == HttpMethod.Options.Method)
            {
                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Access-Control-Allow-Origin", "*"); // O especifica tu dominio
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                return response;
            }

            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.Headers.Add("Access-Control-Allow-Origin", "*"); // O especifica tu dominio
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                CL_LOGINTERFASES datosBody = JsonConvert.DeserializeObject<CL_LOGINTERFASES>(requestBody);
                string parametros = $"WHERE Ejercicio ={datosBody.Ejercicio} AND Periodo ='{datosBody.Periodo}'";

                List<CL_LOGINTERFASES> lista = await _dao.getRowsParams<CL_LOGINTERFASES>(NOMBRE_TABLA, parametros);

                if (lista.Count > 0)
                {
                    string jsonResult = JsonConvert.SerializeObject(lista);
                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync(jsonResult);
                }
                else
                {
                    response.StatusCode = HttpStatusCode.NoContent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al procesar la solicitud: {Message}", ex.Message);
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(ex.Message);
            }
            finally
            {
                _logger.LogInformation("Fin de la función GetAllRows_LOGINTERFASES");
            }

            return response;
        }
        #endregion





    }
}
