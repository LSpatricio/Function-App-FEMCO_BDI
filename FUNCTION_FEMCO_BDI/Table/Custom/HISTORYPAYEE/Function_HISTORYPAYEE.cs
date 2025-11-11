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
using System.Globalization;
using Azure;
using FUNCTION_FEMCO_BDI.Funcionalidades;

namespace FUNCTION_FEMCO_BDI.Table.Custom.HISTORYPAYEE
{
    public class Function_HISTORYPAYEE
    {
        private readonly ILogger _logger;
        private readonly ICMService _icmservice;
        private readonly DAO_SQL _dao;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.HISTORYPAYEE";

        public Function_HISTORYPAYEE(ILoggerFactory loggerFactory,DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_HISTORYPAYEE>();
            _dao = dao;
            _icmservice = icmService;
        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_HISTORYPAYEE()
        {
            DataTable dtfechas = FuncionalidadICM.getdates();

            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

            // Formato MM/dd/yyyystring
            string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);             

            
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoEP");
            string TablaICM = "HistoryPayee";

            string ConsultaICM = @"SELECT PayeeID,
                                            Parent,
                                            TitleID,
                                            ReportsTo,
                                            DateStart,
                                            DateEnd,
                                            IDOrganizationalUnit,
                                            IDSociety,
                                            IDPersonalDivision,
                                            IDPersonalSubdivision,
                                            IDEEGroup,
                                            IDPersonalArea,
                                            IDPayrollArea,
                                            IDPosition,
                                            IDJobKey,
                                            IDCostCenter,
                                            IDAuxiliaryCeco,
                                            IDStore,
                                            IDRole,
                                            DateInsertion,
                                            IDStatus
                                       FROM " + TablaICM;

            string parametros = $@" WHERE \""DateStart\"" >= '{dateStartFormatted}' ";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;



        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_HISTORYPAYEE")]
        public async Task<HttpResponseData> BulkCreate_Trigger_HISTORYPAYEE([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_HISTORYPAYEE")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_HISTORYPAYEE.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_HISTORYPAYEE();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_HISTORYPAYEE");

            }

            return response;

        }
        #endregion


        //#region BulkCreate como Azure Function Timer 

        ////Todos los dias 4:30am
        //[Function("BulkCreate_Timer_HISTORYPAYEE_DailyTask")]
        //public async Task BulkCreate_Timer_HISTORYPAYEE_DailyTask([TimerTrigger("0 30 4 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_HISTORYPAYEE_DailyTask.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_HISTORYPAYEE();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_HISTORYPAYEE_DailyTask: {Message}", ex.Message);


        //    }


        //}


        ////Lunes y Viernes 7:30am hasta las 11:30 pm, cada hora. 
        //[Function("BulkCreate_Timer_HISTORYPAYEE_MondayFridayTask")]
        //public async Task BulkCreate_Timer_HISTORYPAYEE_MondayFridayTask([TimerTrigger("0 30 7-23 * * 1,5")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_HISTORYPAYEE_MondayFridayTask.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_HISTORYPAYEE();
        //        _logger.LogInformation(mensaje);

        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_HISTORYPAYEE_MondayFridayTask: {Message}", ex.Message);


        //    }


        //}
        //#endregion

    }
}
