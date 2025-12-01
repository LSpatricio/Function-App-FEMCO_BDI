using FUNCTION_FEMCO_BDI.Funcionalidades;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        public async Task<DataTable> ConsultarICM(string tablaICM, string consultaOriginal, string modelo, string parametros = "")
        {
            if (string.IsNullOrWhiteSpace(tablaICM))
            {
                throw new ArgumentException("El nombre de la tabla ICM no puede ser nulo o vacío.", nameof(tablaICM));
            }

            try
            {
                // Obtener encabezados
                
                string requestUrlEncabezados = $"{ICMBaseUrl}/customtables/{tablaICM}/inputforms/0/data";

                var requestEncabezados = new HttpRequestMessage(HttpMethod.Get, requestUrlEncabezados);
                requestEncabezados.Headers.Add("Model", modelo);
                HttpResponseMessage encabezadosResponse = await _httpClient.SendAsync(requestEncabezados);

               // HttpResponseMessage encabezadosResponse = await _httpClient.GetAsync(requestUrlEncabezados);

                if (!encabezadosResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error al obtener los encabezados: {encabezadosResponse.StatusCode}");
                }

                JObject jsonEncabezado = JObject.Parse(await encabezadosResponse.Content.ReadAsStringAsync());

                // Obtener datos
                string consultaAjustada = FuncionalidadICM.AjustarConsulta(consultaOriginal);
                string requestUrlDatos = $"{ICMBaseUrl}/imports/getdbpreview";
                string body = $@"
                                {{
                                    ""importParams"": {{
                                        ""query"": ""{consultaAjustada} {parametros}"",
                                        ""model"":""{modelo}"",
                                        ""filename"": null,
                                        ""hasHeader"": null,
                                        ""queryTimeout"": 900,
                                        ""importType"": ""DBImport""
                                    }},
                                    ""numLines"": 999999999
                                }}";

                var requestContenido = new HttpRequestMessage(HttpMethod.Post, requestUrlDatos)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                requestContenido.Headers.Add("Model", modelo);

                HttpResponseMessage contenidoResponse = await _httpClient.SendAsync(requestContenido);


                if (!contenidoResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error al obtener los datos: {contenidoResponse.StatusCode}");
                }

                JArray jsonContenido = JArray.Parse(await contenidoResponse.Content.ReadAsStringAsync());

                // Convertir Json a DataTable
                return FuncionalidadICM.ICMToDataTable(jsonEncabezado, jsonContenido);
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
                throw new InvalidOperationException($"Error en ConsultarICM", ex);

            }
        }




        public async Task<DataTable> ConsultarICM(string tablaICM, string consultaOriginal, string modelo,DataTable dt, string parametros = "")
        {
            if (string.IsNullOrWhiteSpace(tablaICM))
            {
                throw new ArgumentException("El nombre de la tabla ICM no puede ser nulo o vacío.", nameof(tablaICM));
            }

            try
            {

                // Obtener datos
                string consultaAjustada = FuncionalidadICM.AjustarConsulta(consultaOriginal);
                string requestUrlDatos = $"{ICMBaseUrl}/imports/getdbpreview";
                string body = $@"
                                {{
                                    ""importParams"": {{
                                        ""query"": ""{consultaAjustada} {parametros}"",
                                        ""model"":""{modelo}"",
                                        ""filename"": null,
                                        ""hasHeader"": null,
                                        ""queryTimeout"": 900,
                                        ""importType"": ""DBImport""
                                    }},
                                    ""numLines"": 999999999
                                }}";

                var requestContenido = new HttpRequestMessage(HttpMethod.Post, requestUrlDatos)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                requestContenido.Headers.Add("Model", modelo);

                HttpResponseMessage contenidoResponse = await _httpClient.SendAsync(requestContenido);


                if (!contenidoResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error al obtener los datos: {contenidoResponse.StatusCode}");
                }

                JArray jsonContenido = JArray.Parse(await contenidoResponse.Content.ReadAsStringAsync());

                // Convertir Json a DataTable
                return FuncionalidadICM.ICMToDataTable(dt,jsonContenido);
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
                throw new InvalidOperationException($"Error en ConsultarICM", ex);

            }
        }
    }
}
