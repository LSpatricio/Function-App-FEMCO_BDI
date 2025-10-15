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


namespace FUNCTION_FEMCO_BDI.Table.Custom.FEMCOEP_CATPERSONALDIVISION
{
    public class Function_FEMCOEP_CATPERSONALDIVISION
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;
        private const string NOMBRE_TABLA = "FEMCOEPSAP.FEMCOEP_CATPERSONALDIVISION";

        public Function_FEMCOEP_CATPERSONALDIVISION(ILoggerFactory loggerFactory,DAO_SQL dao, ICMService icmService)
        {
            _dao = dao;
            _icmservice = icmService;
            _logger = loggerFactory.CreateLogger<Function_FEMCOEP_CATPERSONALDIVISION>();


        }

        #region BulkCreate como método.
        public async Task BulkCreate_FEMCOEP_CATPERSONALDIVISION()
        {
            string TablaICM = "Nombre Tabla ICM";

            string ConsultaICM = @"SELECT IDPersonalDivision,
                                                IDSociety,
                                                Enviado
                                                 FROM " + TablaICM;

            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM,"");

            //  await _dao.bulkInsert(dt, NOMBRE_TABLA);


        }
        #endregion

        #region BulkCreate como Azure Function HTTPTrigger.

        //[Function("BulkCreate_Trigger_FEMCOEP_CATPERSONALDIVISION")]
        //public async Task<HttpResponseData> BulkCreate_Trigger_FEMCOEP_CATPERSONALDIVISION([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_FEMCOEP_CATPERSONALDIVISION")] HttpRequestData req)
        //{
        //    _logger.LogInformation("Inicio de la función BulkCreate_Trigger_FEMCOEP_CATPERSONALDIVISION.");
        //    var response = req.CreateResponse();
        //    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        //    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();


        //    try
        //    {
        //        await BulkCreate_FEMCOEP_CATPERSONALDIVISION();
        //        response.StatusCode = HttpStatusCode.Created;
        //        await response.WriteStringAsync("Created succesfully");

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Ocurrió un error al procesar la solicitud: {Message}", ex.Message);
        //        response.StatusCode = HttpStatusCode.InternalServerError;
        //        await response.WriteStringAsync(ex.Message);

        //    }
        //    finally
        //    {
        //        _logger.LogInformation("Fin de la función BulkCreate_Trigger_FEMCOEP_CATPERSONALDIVISION");

        //    }

        //    return response;

        //}
        #endregion


        //#region BulkCreate como Azure Function Timer.

        ////Todos los miercoles, inicio a las 11:30 PM

        ////[Function("BulkCreate_Timer_FEMCOEP_CATPERSONALDIVISION_Wednesday")]
        ////public async Task BulkCreate_Timer_FEMCOEP_CATPERSONALDIVISION_Wednesday([TimerTrigger("0 30 23 * * 3")] TimerInfo myTimer)
        ////{

        ////    _logger.LogInformation("Inicio de la función BulkCreate_Timer_FEMCOEP_CATPERSONALDIVISION_Wednesday.");


        ////    try
        ////    {
        ////        await BulkCreate_FEMCOEP_CATPERSONALDIVISION();

        ////    }
        ////    catch (Exception ex)
        ////    {

        ////        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_FEMCOEP_CATPERSONALDIVISION_Wednesday: {Message}", ex.Message);


        ////    }


        ////}



        ////Todos los jueves, inicio a las 12:30 AM hasta las 11:30 PM

        ////[Function("BulkCreate_Timer_FEMCOEP_CATPERSONALDIVISION_Thursday")]
        ////public async Task BulkCreate_Timer_FEMCOEP_CATPERSONALDIVISION_Thursday([TimerTrigger("0 30 0-23 * * 4")] TimerInfo myTimer)
        ////{

        ////    _logger.LogInformation("Inicio de la función BulkCreate_Timer_FEMCOEP_CATPERSONALDIVISION_Thursday.");


        ////    try
        ////    {
        ////        await BulkCreate_FEMCOEP_CATPERSONALDIVISION();

        ////    }
        ////    catch (Exception ex)
        ////    {

        ////        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_FEMCOEP_CATPERSONALDIVISION_Thursday: {Message}", ex.Message);


        ////    }


        ////}
        //#endregion


    }
}
