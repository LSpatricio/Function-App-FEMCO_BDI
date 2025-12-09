using FUNCTION_FEMCO_BDI.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using System.Globalization;

namespace FUNCTION_FEMCO_BDI.Table.Custom.DATACATEGORYSALES
{
    public class Function_DATACATEGORYSALES
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOCOMP.DATACATEGORYSALES";

        public Function_DATACATEGORYSALES(ILoggerFactory loggerFactory,DAO_SQL dao,ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_DATACATEGORYSALES>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_DATACATEGORYSALES()
        {
            DataTable dtfechas = FuncionalidadICM.getdates();
            //DataTable dtfechas = FuncionalidadICM.getdates(14);

            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

            // Formato MM/dd/yyyystring
            string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoComp");
            string TablaICM = "DataCategorySales";

            List<string> columnas = new List<string>
            {
                "IDStore",
                "Date",
                "IDSuperGroup",
                "IDCategory",
                "IDSubCategory",
                "IDSegment",
                "IDSubSegment",
                "Amount",
                "Quantity"
            };
            string parametros = $@" WHERE \""Date\"" >= '{dateStartFormatted}' ";
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

            for (int i = 0; i < count; i += 500000)
            {
                DataTable dtParte = await _icmservice.ConsultaICMQuerytool(TablaICM, $"{consultaICM} {orderBy}", modeloICM, i);
                mensaje = await _dao.bulkInsert(dtParte, NOMBRE_TABLA);
            }


            return mensaje;


        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_DATACATEGORYSALES")]
        public async Task<HttpResponseData> BulkCreate_Trigger_DATACATEGORYSALES([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_DATACATEGORYSALES")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_DATACATEGORYSALES.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_DATACATEGORYSALES();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_DATACATEGORYSALES");

            }

            return response;

        }
        #endregion


        #region BulkCreate como Azure Function Timer.

        //1 vez al mes, el dia 12 una ejecución.

        [Function("BulkCreate_Timer_DATACATEGORYSALES_Day12")]
        public async Task BulkCreate_Timer_DATACATEGORYSALES_Day12([TimerTrigger("0 50 16 9 12 *")] TimerInfo myTimer)
        {

            _logger.LogInformation("Inicio de la función BulkCreate_Timer_DATACATEGORYSALES_Day12.");


            try
            {
                string mensaje = await BulkCreate_DATACATEGORYSALES();
                _logger.LogInformation(mensaje);


            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_DATACATEGORYSALES_Day12: {Message}", ex.Message);


            }


        }

        //1 vez al mes, el dia 13 cada 4 horas, desde las 3:30 AM hasta las 7:30 PM

        [Function("BulkCreate_Timer_DATACATEGORYSALES_Day13")]
        public async Task BulkCreate_Timer_DATACATEGORYSALES_Day13([TimerTrigger("0 30 3,7,11,15,19 13 * *")] TimerInfo myTimer)
        {

            _logger.LogInformation("Inicio de la función BulkCreate_Timer_DATACATEGORYSALES_Day13.");


            try
            {
                string mensaje = await BulkCreate_DATACATEGORYSALES();
                _logger.LogInformation(mensaje);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_DATACATEGORYSALES_Day13: {Message}", ex.Message);


            }


        }
        #endregion



    }
}
