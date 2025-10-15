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

        [Function("FunctionTestTNC")]
        public async Task<HttpResponseData> FunctionTestTNC([HttpTrigger(AuthorizationLevel.Function, "get", Route= "FunctionTestTNC")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function for TNC processed a request.");

            // Obtener el host y el puerto de SQL Server desde el request o configuración
            string server = Environment.GetEnvironmentVariable("SqlServerHost") ?? "vmazceicmd01.csc.fmx";
            int port = int.TryParse(Environment.GetEnvironmentVariable("SqlServerPort") ?? "51452", out int parsedPort) ? parsedPort : 51452;

            HttpResponseData response;

            try
            {
                // Realizar la prueba de conectividad
                using (TcpClient tcpClient = new TcpClient())
                {
                    _logger.LogInformation($"Intentando conectar a {server}:{port}...");
                    var connectTask = tcpClient.ConnectAsync(server, port);
                    var timeoutTask = Task.Delay(5000); // Timeout de 5 segundos

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && tcpClient.Connected)
                    {
                        _logger.LogInformation($"Conexión exitosa a {server}:{port}");
                        response = req.CreateResponse(HttpStatusCode.OK);
                        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

                        string successMessage = $"{{ \"message\": \"Conexión TNC exitosa.\", \"server\": \"{server}\", \"port\": {port}, \"timestamp\": \"{DateTime.UtcNow}\" }}";
                        await response.WriteStringAsync(successMessage);
                    }
                    else
                    {
                        _logger.LogWarning($"No se pudo conectar a {server}:{port} dentro del tiempo límite.");
                        response = req.CreateResponse(HttpStatusCode.RequestTimeout);
                        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

                        string timeoutMessage = $"{{ \"error\": \"Timeout al intentar conectar.\", \"server\": \"{server}\", \"port\": {port}, \"timestamp\": \"{DateTime.UtcNow}\" }}";
                        await response.WriteStringAsync(timeoutMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al intentar conectar a {server}:{port}: {ex.Message}");

                // Crear una respuesta de error
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");

                string errorMessage = $"{{ \"error\": \"Error al intentar conectar.\", \"details\": \"{ex.Message}\", \"server\": \"{server}\", \"port\": {port}, \"timestamp\": \"{DateTime.UtcNow}\" }}";
                await response.WriteStringAsync(errorMessage);
            }

            return response;
        }


    }
}
