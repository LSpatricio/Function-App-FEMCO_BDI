using FUNCTION_FEMCO_BDI.DAO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using System.Globalization;

namespace FUNCTION_FEMCO_BDI.Table.Custom.CFGDATESTRINGPERIOD
{
    public class Function_CFGDATESTRINGPERIOD
    {

        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.CFGDATESTRINGPERIOD";
        private readonly ILogger _logger;

        public Function_CFGDATESTRINGPERIOD(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmservice)
        {
            _logger = loggerFactory.CreateLogger<Function_CFGDATESTRINGPERIOD>();
            _dao = dao;
            _icmservice = icmservice;


        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_CFGDATESTRINGPERIOD()
        {

            DataTable dtfechas = FuncionalidadICM.getdates();

            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

            // Formato MM/dd/yyyystring
            string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

           
            //**********Obtencion de los datos en un datatable.************************************
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "CfgDateStringPeriod";

            List<string> columnas = new List<string>
            {
                "IDPeriod",
                "PeriodName",
                "Year",
                "Month",
                "Week",
                "DateStart",
                "DateEnd",
                "PeriodNumber",
                "NumOfDaysMonth",
                "PriorMonth",
                "PriorYear",
                "PriorYearPeriod",
                "NextPeriod",
                "NextYearPeriod",
                "IsNextToLastPeriod",
                "IsLastPeriod",
                "IsOutputInterface",
                "PeriodText",
                "WeekNumber",
                "NextWeekNumber",
                "YearNumber",
                "NextYearNumber",
                "PriorPeriodName",
                "PriorPeriodNumber"
            };
            string parametros = $@" WHERE \""DateStart\"" >= '{dateStartFormatted}'  ";
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

        [Function("BulkCreate_Trigger_CFGDATESTRINGPERIOD")]
        public async Task<HttpResponseData> BulkCreate_Trigger_CFGDATESTRINGPERIOD([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_CFGDATESTRINGPERIOD")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_CFGDATESTRINGPERIOD.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_CFGDATESTRINGPERIOD();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_CFGDATESTRINGPERIOD");

            }

            return response;

        }
        #endregion


        #region BulkCreate como Azure Function Timer.

        //Se ejecuta diario a las 8:30 AM y 3:30 PM
        [Function("BulkCreate_Timer_CFGDATESTRINGPERIOD")]
        public async Task BulkCreate_Timer_CFGDATESTRINGPERIOD([TimerTrigger("0 30 8,15 * * *")] TimerInfo myTimer)
        {

            _logger.LogInformation("Inicio de la función BulkCreate_Timer_CFGDATESTRINGPERIOD.");


            try
            {
                string mensaje = await BulkCreate_CFGDATESTRINGPERIOD();
                _logger.LogInformation(mensaje);


            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_CFGDATESTRINGPERIOD: {Message}", ex.Message);


            }


        }

        #endregion
    }



}
