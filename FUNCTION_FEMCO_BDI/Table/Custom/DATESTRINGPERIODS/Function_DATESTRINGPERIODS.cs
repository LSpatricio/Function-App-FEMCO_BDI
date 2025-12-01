using FUNCTION_FEMCO_BDI.DAO;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.Table.Custom.DATESTRINGPERIODS
{
    public class Function_DATESTRINGPERIODS
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCO.DATESTRINGPERIODS";

        public Function_DATESTRINGPERIODS(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_DATESTRINGPERIODS>();
            _dao = dao;
            _icmservice = icmService;
        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_DATESTRINGPERIODS()
        {
            DataTable dtfechas = FuncionalidadICM.getdates();

            DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

            // Formato MM/dd/yyyystring
            string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

           
            //**********Obtencion de los datos en un datatable.************************************
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemco");
            string TablaICM = "DateStringPeriods";

            string ConsultaICM = @"SELECT PeriodString,
                                                PeriodName,
                                                StarDate,
                                                EndDate,
                                                QuaterString,
                                                NumOfWeeks,
                                                NumOfDays,
                                                MonthInYear,
                                                MonthInYearString,
                                                MonthInYearString0,
                                                MonthInQtr,
                                                MonthInQtrString,
                                                MonthName,
                                                MonthNameAbbr,
                                                PeriodIncrement,
                                                PriorPeriod,
                                                NextPeriod,
                                                PriorYearPeriod,
                                                NextYearPeriod,
                                                ReportDisplayLabel,
                                                IsReportDisplay,
                                                IsOutputInterface
                                                 FROM " + TablaICM;

            string parametros = "  ";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;



        }
        #endregion


        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_DATESTRINGPERIODS")]
        public async Task<HttpResponseData> BulkCreate_Trigger_DATESTRINGPERIODS([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_DATESTRINGPERIODS")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_DATESTRINGPERIODS.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_DATESTRINGPERIODS();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_DATESTRINGPERIODS");

            }

            return response;

        }
        #endregion


        //#region BulkCreate como Azure Function Timer.

        ////Se ejecuta diario a las 8:30 AM y 3:30 PM
        //[Function("BulkCreate_Timer_DATESTRINGPERIODS")]
        //public async Task BulkCreate_Timer_DATESTRINGPERIODS_DailyTask([TimerTrigger("0 30 8,15 * * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_DATESTRINGPERIODS_DailyTask.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_DATESTRINGPERIODS()
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_DATESTRINGPERIODS_DailyTask: {Message}", ex.Message);


        //    }


        //}

        //#endregion
    }



    //#region Bulk Insert a la tabla

    //[Function("BulkCreate_DATESTRINGPERIODS")]
    //    public async Task<HttpResponseData> BulkCreate_DATESTRINGPERIODS([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_DATESTRINGPERIODS")] HttpRequestData req)
    //    {
    //        _logger.LogInformation("Inicio de la función BulkCreate_DATESTRINGPERIODS.");
    //        var response = req.CreateResponse();
    //        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
    //        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();


    //        try
    //        {
    //            DataTable dataTable = JsonConvert.DeserializeObject<DataTable>(requestBody);

    //            await _dao.bulkInsert(dataTable, NOMBRE_TABLA);
    //            response.StatusCode = HttpStatusCode.Created;
    //            await response.WriteStringAsync("Created succesfully");

    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Ocurrió un error al procesar la solicitud: {Message}", ex.Message);
    //            response.StatusCode = HttpStatusCode.InternalServerError;
    //            await response.WriteStringAsync(ex.Message);

    //        }
    //        finally
    //        {
    //            _logger.LogInformation("Fin de la función BulkCreate_DATESTRINGPERIODS");

    //        }

    //        return response;

    //    }
    //    #endregion

    //    #region Select a tabla

    //    [Function("GetAllRows_DATESTRINGPERIODS")]
    //    public async Task<HttpResponseData> GetAllRows_DATESTRINGPERIODS([HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetAllRows_DATESTRINGPERIODS")] HttpRequestData req)
    //    {
    //        _logger.LogInformation("Inicio de la función GetAllRows_DATESTRINGPERIODS.");

    //        var response = req.CreateResponse();
    //        response.Headers.Add("Content-Type", "application/json; charset=utf-8");


    //        try
    //        {
    //            List<CL_DATESTRINGPERIODS> lista = await _dao.getAllRows<CL_DATESTRINGPERIODS>(NOMBRE_TABLA);

    //            if (lista.Count > 0)
    //            {
    //                string jsonResult = JsonConvert.SerializeObject(lista);
    //                response.StatusCode = HttpStatusCode.OK;
    //                await response.WriteStringAsync(jsonResult);
    //            }
    //            else
    //            {
    //                response.StatusCode = HttpStatusCode.NoContent;
    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Ocurrió un error al procesar la solicitud: {Message}", ex.Message);
    //            response.StatusCode = HttpStatusCode.InternalServerError;
    //            await response.WriteStringAsync(ex.Message);
    //        }
    //        finally
    //        {
    //            _logger.LogInformation("Fin de la función GetAllRows_DATESTRINGPERIODS");

    //        }


    //        return response;


    //    }
    //    #endregion

    //    #region Eliminar registros

    //    [Function("Delete_DATESTRINGPERIODS")]
    //    public async Task<HttpResponseData> Delete_DATESTRINGPERIODS([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Delete_DATESTRINGPERIODS")] HttpRequestData req)
    //    {


    //        _logger.LogInformation("Inicio de la función Delete_DATESTRINGPERIODS.");
    //        var response = req.CreateResponse();
    //        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

    //        try
    //        {
    //            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    //            CL_DATESTRINGPERIODS datosBody = JsonConvert.DeserializeObject<CL_DATESTRINGPERIODS>(requestBody);

    //            int r = await _dao.deleteRangeDates(NOMBRE_TABLA, datosBody.StarDate, datosBody.EndDate, nameof(datosBody.StarDate), nameof(datosBody.EndDate));

    //            if (r > 0)
    //            {
    //                response.StatusCode = HttpStatusCode.OK;
    //                await response.WriteStringAsync("Records deleted successfully.");
    //            }
    //            else
    //            {
    //                response.StatusCode = HttpStatusCode.NotFound;
    //                await response.WriteStringAsync("No records found.");

    //            }

    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Ocurrió un error al procesar la solicitud: {Message}", ex.Message);
    //            response.StatusCode = HttpStatusCode.InternalServerError;
    //            await response.WriteStringAsync(ex.Message);
    //        }
    //        finally
    //        {
    //            _logger.LogInformation("Fin de la función Delete_DATESTRINGPERIODS.");
    //        }

    //        return response;
    //    }
    //    #endregion





    //}
}
