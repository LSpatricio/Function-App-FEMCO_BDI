using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;

namespace FUNCTION_FEMCO_BDI.Funcionalidades.TestConnection
{
    public class FunctionTestPing
    {
        private readonly ILogger _logger;

        public FunctionTestPing(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FunctionTestPing>();
        }

        [Function("FunctionTestPing")]
        public async Task<HttpResponseData> TestPing(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "TestPing")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Obtener la cadena de conexión desde las variables de entorno
            string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            HttpResponseData response;

            try
            {
                // Intentar abrir una conexión al servidor SQL
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation($"Conexión exitosa al servidor SQL: {connection.DataSource}");

                    // Crear una respuesta exitosa
                    response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json; charset=utf-8");

                    string successMessage = $"{{ \"message\": \"Ping exitoso al servidor SQL.\", \"server\": \"{connection.DataSource}\", \"timestamp\": \"{DateTime.UtcNow}\" }}";
                    await response.WriteStringAsync(successMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al conectar con el servidor SQL: {ex.Message}");

                // Crear una respuesta de error
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");

                string errorMessage = $"{{ \"error\": \"Error al conectar con el servidor SQL.\", \"details\": \"{ex.Message}\", \"timestamp\": \"{DateTime.UtcNow}\" }}";
                await response.WriteStringAsync(errorMessage);
            }

            return response;
        }

     
    }
}
