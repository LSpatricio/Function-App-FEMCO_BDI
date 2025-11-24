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

namespace FUNCTION_FEMCO_BDI.Table.Custom.AUDIT_
{
    public class Function_AUDIT_
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.AUDIT_";

        public Function_AUDIT_(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_AUDIT_>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_AUDIT_()
        {

            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "Audit_";
            string ConsultaICM = @"SELECT AuditID_,
                                            UserType_,
                                            UserID_,
                                            Module_,
                                            Event_,
                                            Time_,
                                            Message_,
                                            _Start,
                                            _End
                                            FROM " + TablaICM;

            string parametros = "";

            DataTable dt = new DataTable();
            dt.Columns.Add("AuditID_", typeof(decimal));
            dt.Columns.Add("UserType_", typeof(decimal));
            dt.Columns.Add("UserID_", typeof(string));
            dt.Columns.Add("Module_", typeof(string));
            dt.Columns.Add("Event_", typeof(string));
            dt.Columns.Add("Time_", typeof(DateTime));
            dt.Columns.Add("Message_", typeof(string));
            dt.Columns.Add("_Start", typeof(decimal));
            dt.Columns.Add("_End", typeof(decimal));

            dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, dt, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;

        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_AUDIT_")]
        public async Task<HttpResponseData> BulkCreate_Trigger_AUDIT_([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_AUDIT_")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_AUDIT_.");
            var response = req.CreateResponse();

            try
            {
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string mensaje = await BulkCreate_AUDIT_();

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
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(ex.Message);

            }
            finally
            {
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_AUDIT_");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Diario dos ejecuciones. A las 8:30 am y 3:30 pm

        //[Function("BulkCreate_Timer_AUDIT_")]
        //public async Task BulkCreate_Timer_AUDIT_DailyTask([TimerTrigger("0 30 8,15 * * *")] TimerInfo myTimer)
        //{
        //    //Expresion cron
        //    //azure
        //    // segundos , minutos, horas, dias, mes, dia (Lunes a Domingo)

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_AUDIT_");


        //    try
        //    {
        //        string mensaje = await BulkCreate_AUDIT_();
        //        _logger.LogInformation(mensaje);

        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_AUDIT: {Message}", ex.Message);


        //    }


        //}


        //#endregion



    }
}
