using FUNCTION_FEMCO_BDI.DTOs;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.DAO
{
    public class ICMService
    {
        private readonly HttpClient _httpClient;
        string ICMBaseUrl = Environment.GetEnvironmentVariable("ICMBaseUrl");
        

        public ICMService(HttpClient httpClient)
        {
         
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
        }

        /// <summary>
        /// Ejecución de consulta a ICM, devuelve un DataTable
        /// </summary>
        /// <param name="tablaICM"></param>
        /// <param name="consultaOriginal"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TimeoutException"></exception>
        public async Task<DataTable> ConsultaICMQuerytool(string tablaICM, string consulta, string modelo, int offset)
        {
            if (string.IsNullOrWhiteSpace(tablaICM))
            {
                throw new ArgumentException("El nombre de la tabla ICM no puede ser nulo o vacío.", nameof(tablaICM));
            }

            try
            {
                DataTable dt;

                HttpResponseMessage contenidoResponse =await ConstruirResquestQueryTool(consulta, offset, modelo);


                if (!contenidoResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error al obtener los datos: {contenidoResponse.StatusCode}");
                }
                JObject jsoncontendio ;
                using (var respuestaStream = await contenidoResponse.Content.ReadAsStreamAsync()) 
                using (var sr = new System.IO.StreamReader(respuestaStream, Encoding.UTF8))
                using (var jsonReader = new Newtonsoft.Json.JsonTextReader(sr))
                {
                    jsoncontendio = JObject.Load(jsonReader);
                    
                }

                JArray columnDefinitions = (JArray)jsoncontendio["columnDefinitions"];
        
                dt = FuncionalidadICM.CrearColumnasQuerytool(columnDefinitions);

                //Ahora recorrer el data.
                JArray data = (JArray)jsoncontendio["data"];

                FuncionalidadICM.LlenarDataTableQuerytool(data, dt);

                return dt; 
            }
            catch (HttpRequestException ex)
            {
                // Manejo de errores relacionados con HTTP
                Console.WriteLine($"Error al realizar la solicitud HTTP: {ex.Message}");
                throw new InvalidOperationException("Ocurrió un error al comunicarse con el servicio ICM.", ex);
            }
            catch (TaskCanceledException ex)
            {
                // Manejo de tiempo de espera (timeout)
                Console.WriteLine($"Solicitud cancelada o excedió el tiempo de espera: {ex.Message}");
                throw new TimeoutException("La solicitud tardó demasiado y fue cancelada.", ex);
            }
            catch (Exception ex)
            {
                // Manejo de cualquier otro tipo de excepción
                Console.WriteLine($"Ocurrió un error inesperado: {ex.Message}");
                throw new InvalidOperationException($"Error en ConsultarICM: {ex.Message}", ex);

            }
        }


        private async Task<HttpResponseMessage> ConstruirResquestQueryTool(string consulta, int offset, string modelo)
        {

            string requestUrlDatos = $"{ICMBaseUrl}/rpc/querytool";
            string body = $@"
                                    {{
                                        ""queryString"": ""{consulta}"",
                                        ""offset"":{offset},
                                        ""limit"": 400000
                                    }}";

            var requestContenido = new HttpRequestMessage(HttpMethod.Post, requestUrlDatos)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            requestContenido.Headers.Add("Model", modelo);

            HttpResponseMessage contenidoResponse = await _httpClient.SendAsync(requestContenido, HttpCompletionOption.ResponseHeadersRead);

            return contenidoResponse;
        }

        public async Task<RunScheduleitemResponse> EjecutarScheduleitem(string itemId, string modelo)
        {
            try 
            { 
            
            string requestUrl = $"{ICMBaseUrl}/rpc/scheduleitem/{itemId}/run";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            request.Headers.Add("Model", modelo);

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.ServiceUnavailable: // 503
                        throw new HttpRequestException(
                            "No se puede ejecutar la importación. La tabla está bloqueada por otro proceso.");

                    case HttpStatusCode.Conflict: // 409
                        throw new HttpRequestException(
                            "No se puede ejecutar la importación. Hay otros procesos en ejecución.");

                    default:
                        throw new HttpRequestException(
                            $"Error al ejecutar schedule item: {response.StatusCode}");
                }


            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            RunScheduleitemResponse runScheduleitemResponse = JsonConvert.DeserializeObject<RunScheduleitemResponse>(jsonResponse);

            if (runScheduleitemResponse == null)
            {
                throw new InvalidOperationException("La respuesta del servicio ICM no se pudo deserializar correctamente.");
            }

            return runScheduleitemResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error inesperado: {ex.Message}");

                throw new InvalidOperationException(
                    "Error inesperado al consultar ICM.", ex);
            }
       

        }

        public async Task<LiveActivitiesResponse> ConsultarLiveActivitie(string runId, string modelo)
        {

            try
            {
                string requestUrl = $"{ICMBaseUrl}/liveactivities?filter=progressId={runId}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                request.Headers.Add("Model", modelo);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error al consultar live activities: {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                List<LiveActivitiesResponse> liveActiviesResponseArray = JsonConvert.DeserializeObject<List<LiveActivitiesResponse>>(jsonResponse);

                LiveActivitiesResponse liveActiviesResponse = liveActiviesResponseArray.FirstOrDefault();

                return liveActiviesResponse;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error inesperado: {ex.Message}");

                throw new InvalidOperationException(
                    "Error inesperado al consultar ICM.", ex);
            }
            

        }

        public async Task<CompletedActivitiesResponse> ConsultarCompletedActivitie(string runId, string modelo)
        {
            try
            {
                string requestUrl = $"{ICMBaseUrl}/completedactivities?filter=progressId={runId}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                request.Headers.Add("Model", modelo);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error al consultar completedactivities activities: {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                List<CompletedActivitiesResponse> completedActiviesResponseArray = JsonConvert.DeserializeObject<List<CompletedActivitiesResponse>>(jsonResponse);

                CompletedActivitiesResponse completedActiviesResponse = completedActiviesResponseArray.FirstOrDefault();

                return completedActiviesResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error inesperado: {ex.Message}");

                throw new InvalidOperationException(
                    "Error inesperado al consultar ICM.", ex);
            }
            


        }


    }
}
