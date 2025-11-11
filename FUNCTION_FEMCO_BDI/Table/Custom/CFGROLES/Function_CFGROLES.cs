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


namespace FUNCTION_FEMCO_BDI.Table.Custom.CFGROLES
{
    public class Function_CFGROLES
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.CFGROLES";

        public Function_CFGROLES(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_CFGROLES>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_CFGROLES()
        {
            DataTable dtfechas = FuncionalidadICM.getdates();

            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

            // Formato MM/dd/yyyystring
            string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "CfgRoles";
            string ConsultaICM = @"SELECT IDSociety
                                          ,IDRole
                                          ,IDJobKey
                                          ,DateStart
                                          ,DateEnd
                                       FROM " + TablaICM;

            string parametros = $@" WHERE \""DateStart\"" >= '{dateStartFormatted}'";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;


        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_CFGROLES")]
        public async Task<HttpResponseData> BulkCreate_Trigger_CFGROLES([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_CFGROLES")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                _logger.LogInformation("Inicio de la función BulkCreate_Trigger_CFGROLES.");
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                string mensaje = await BulkCreate_CFGROLES();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_CFGROLES");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Todos los dias 3:00 pm
        //[Function("BulkCreate_Timer_CFGROLES")]
        //public async Task BulkCreate_Timer_CFGROLES([TimerTrigger("0 0 15 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_CFGROLES.");

        //    try
        //    {
        //        string mensaje = await BulkCreate_CFGROLES();
        //        _logger.LogInformation(mensaje);

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_CFGROLES: {Message}", ex.Message);
        //    }
        //    finally
        //    {
        //        _logger.LogInformation("Fin de la función BulkCreate_Timer_CFGROLES.");
        //    }
        //}

        //#endregion




    }
}
