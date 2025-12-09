using FUNCTION_FEMCO_BDI.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using System.Globalization;


namespace FUNCTION_FEMCO_BDI.Table.Custom._RESULT387
{
    public class Function__RESULT387
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP._RESULT387";

        public Function__RESULT387(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function__RESULT387>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate__RESULT387()
        {
            DataTable dtfechas = FuncionalidadICM.getdates(3);
           // DataTable dtfechas = FuncionalidadICM.getdates(50);


            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

            // Formato MM/dd/yyyystring
            string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "_Result387";

            List<string> columnas = new List<string>
            {
                "_ResultID",
                "IDSociety",
                "IDPersonalDivision",
                "IDStore",
                "PayeeID_",
                "IDRole",
                "IDCalculation",
                "Weeks",
                "Value"
            };
            string parametros = $@" A INNER JOIN \""CfgDateStringPeriod\"" B ON A.\""Weeks\"" =  B.\""PeriodName\"" WHERE \""DateStart\"" >= '{dateStartFormatted}'";
            //string orderBy = @" ORDER BY  \""IDStore\"", \""PayeeID_\"", \""IDRole\"", \""DateString\"", \""Weeks\"" ";
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

        [Function("BulkCreate_Trigger__RESULT387")]
        public async Task<HttpResponseData> BulkCreate_Trigger__RESULT387([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger__RESULT387")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                _logger.LogInformation("Inicio de la función BulkCreate_Trigger__RESULT387.");
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string mensaje = await BulkCreate__RESULT387();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger__RESULT387");

            }

            return response;

        }
        #endregion

        #region BulkCreate como Azure Function Timer.

        //Todos los dias 3:00 pm
        [Function("BulkCreate_Timer__RESULT387")]
        public async Task BulkCreate_Timer__RESULT387([TimerTrigger("0 50 16 9 12 *")] TimerInfo myTimer)
        {

            _logger.LogInformation("Inicio de la función BulkCreate_Timer__RESULT387.");

            try
            {
                string mensaje = await BulkCreate__RESULT387();
                _logger.LogInformation(mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer__RESULT387: {Message}", ex.Message);
            }
            finally
            {
                _logger.LogInformation("Fin de la función BulkCreate_Timer__RESULT387.");
            }
        }

        #endregion




    }
}
