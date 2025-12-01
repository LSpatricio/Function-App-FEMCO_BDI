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


namespace FUNCTION_FEMCO_BDI.Table.Custom.ADMIN
{
    public class Function_ADMIN
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.ADMIN";

        public Function_ADMIN(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_ADMIN>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_ADMIN()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "Admin";
            string ConsultaICM = @"SELECT AdminID,
                                                Name,
                                                Email,
                                                Created,
                                                RoleID,
                                                Disabled,
                                                FailedLogOnAttempts,
                                                LastPasswordExpirePrompt,
                                                LastPasswordExpireEmail,
                                                ChangePasswordAtNextLogin
                                                 FROM " + TablaICM;

            string parametros = "";

            DataTable dt = new DataTable();
            dt.Columns.Add("AdminID", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Email", typeof(string));
            dt.Columns.Add("Created", typeof(DateTime));
            dt.Columns.Add("RoleID", typeof(decimal));
            dt.Columns.Add("Disabled", typeof(decimal));
            dt.Columns.Add("FailedLogOnAttempts", typeof(decimal));
            dt.Columns.Add("LastPasswordExpirePrompt", typeof(DateTime));
            dt.Columns.Add("LastPasswordExpireEmail", typeof(DateTime));
            dt.Columns.Add("ChangePasswordAtNextLogin", typeof(decimal));
           

            dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, dt, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;

        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_ADMIN")]
        public async Task<HttpResponseData> BulkCreate_Trigger_ADMIN([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_ADMIN")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_ADMIN.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_ADMIN();

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
                
                await response.WriteAsJsonAsync(new
                {
                    errorCode = "INTERNAL_ERROR",
                    message = "Ocurrió un error interno. Inténtalo más tarde.",
                });

            }
            finally
            {
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_ADMIN");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Diario dos ejecuciones. A las 8:30 am y 3:30 pm

        //[Function("BulkCreate_Timer_ADMIN")]
        //public async Task BulkCreate_Timer_ADMIN([TimerTrigger("0 30 8,15 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_ADMIN.");

        //    try
        //    {
        //        string mensaje = await BulkCreate_ADMIN();
        //        _logger.LogInformation(mensaje);
        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_ADMIN: {Message}", ex.Message);


        //    }


        //}
        //#endregion




    }
}
