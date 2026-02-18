using FUNCTION_FEMCO_BDI.Funcionalidades;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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


    }
}
