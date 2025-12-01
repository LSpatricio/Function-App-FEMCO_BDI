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


namespace FUNCTION_FEMCO_BDI.Table.Custom.CFGSTOREHIERARCHY
{
    public class Function_CFGSTOREHIERARCHY
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.CFGSTOREHIERARCHY";

        public Function_CFGSTOREHIERARCHY(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_CFGSTOREHIERARCHY>();
            _dao = dao;
            _icmservice = icmService;

        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_CFGSTOREHIERARCHY()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "CfgStoreHierarchy";
            string ConsultaICM = @"SELECT IDStore
                                          ,IDZone
                                          ,IDSociety
                                          ,IDPersonalDivision
                                       FROM " + TablaICM;

            string parametros = "";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;



        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_CFGSTOREHIERARCHY")]
        public async Task<HttpResponseData> BulkCreate_Trigger_CFGSTOREHIERARCHY([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_CFGSTOREHIERARCHY")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_CFGSTOREHIERARCHY.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_CFGSTOREHIERARCHY();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_CFGSTOREHIERARCHY");

            }

            return response;

        }
        #endregion

        //#region BulkCreate como Azure Function Timer.

        ////Todos los dias 3:00 pm
        //[Function("BulkCreate_Timer_CFGSTOREHIERARCHY")]
        //public async Task BulkCreate_Timer_CFGSTOREHIERARCHY([TimerTrigger("0 0 15 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_CFGSTOREHIERARCHY.");

        //    try
        //    {
        //        string mensaje = await BulkCreate_CFGSTOREHIERARCHY();
        //        _logger.LogInformation(mensaje);

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_CFGSTOREHIERARCHY: {Message}", ex.Message);
        //    }
        //    finally
        //    {
        //        _logger.LogInformation("Fin de la función BulkCreate_Timer_CFGSTOREHIERARCHY.");
        //    }
        //}

        //#endregion




    }
}
