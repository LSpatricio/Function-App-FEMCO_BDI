using FUNCTION_FEMCO_BDI.DAO;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Threading.Tasks;


namespace FUNCTION_FEMCO_BDI.Table.Custom.SYSULTIMAIMPORTACIONEXITOSA
{
    public class Function_SYSULTIMAIMPORTACIONEXITOSA
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCO.SYSULTIMAIMPORTACIONEXITOSA";

        public Function_SYSULTIMAIMPORTACIONEXITOSA(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_SYSULTIMAIMPORTACIONEXITOSA>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_SYSULTIMAIMPORTACIONEXITOSA()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemco");
            string TablaICM = "sysUltimaImportacionExitosa";

            List<string> columnas = new List<string>
            {
                "NombreImportacion",
                "Fecha",
                "FechaTexto"
            };
            string parametros = "";
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

        [Function("BulkCreate_Trigger_SYSULTIMAIMPORTACIONEXITOSA")]
        public async Task<HttpResponseData> BulkCreate_Trigger_SYSULTIMAIMPORTACIONEXITOSA([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_SYSULTIMAIMPORTACIONEXITOSA")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                _logger.LogInformation("Inicio de la función BulkCreate_Trigger_SYSULTIMAIMPORTACIONEXITOSA.");
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string mensaje = await BulkCreate_SYSULTIMAIMPORTACIONEXITOSA();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_SYSULTIMAIMPORTACIONEXITOSA");

            }

            return response;

        }
        #endregion

        #region BulkCreate como Azure Function Timer.

        //Diario dos ejecuciones. A las 8:30 am y 3:30 pm

        [Function("BulkCreate_Timer_SYSULTIMAIMPORTACIONEXITOSA")]
        public async Task BulkCreate_Timer_SYSULTIMAIMPORTACIONEXITOSA([TimerTrigger("0 30 8,15 * * *")] TimerInfo myTimer)
        {

            _logger.LogInformation("Inicio de la función BulkCreate_Timer_SYSULTIMAIMPORTACIONEXITOSA.");


            try
            {
                string mensaje = await BulkCreate_SYSULTIMAIMPORTACIONEXITOSA();
                _logger.LogInformation(mensaje);


            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_SYSULTIMAIMPORTACIONEXITOSA: {Message}", ex.Message);


            }


        }
        #endregion




    }
}
