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
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using System.Globalization;

namespace FUNCTION_FEMCO_BDI.Table.Custom.DTOXXOTDAVENTAYMERMAPORESTRUCTURA
{
    public class Function_DTOXXOTDAVENTAYMERMAPORESTRUCTURA
    {
        //private readonly ILogger _logger;
        //private readonly DAO_SQL _dao;
        //private readonly ICMService _icmservice;
        //private const string NOMBRE_TABLA = "FEMCO.DTOXXOTDAVENTAYMERMAPORESTRUCTURA";

        //public Function_DTOXXOTDAVENTAYMERMAPORESTRUCTURA(ILoggerFactory loggerFactory, DAO_SQL dao, ICMService icmService)
        //{
        //    _logger = loggerFactory.CreateLogger<Function_DTOXXOTDAVENTAYMERMAPORESTRUCTURA>();
        //    _dao = dao;
        //    _icmservice= icmService;

        //}

        //#region BulkCreate como método.
        //public async Task<string> BulkCreate_DTOXXOTDAVENTAYMERMAPORESTRUCTURA()
        //{
        //    DataTable dtfechas = FuncionalidadICM.getdates();
        //    //DataTable dtfechas = FuncionalidadICM.getdates(4);


        //    DateTime dateStart = (DateTime)dtfechas.Rows[0]["DateStart"];

        //    // Formato MM/dd/yyyystring
        //    string dateStartFormatted = dateStart.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

        //    //**********Obtencion de los datos en un datatable.************************************
        //    string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoDev");
        //    string tablaICM = "dtOxxoTdaVentaYMermaPorEstructura";
           
        //    string consultaICM = $@"SELECT TiendaID,
        //                                    EmpleadoID,
        //                                    Fecha,
        //                                    SuperGrupoID,
        //                                    CategoriaID,
        //                                    SubcategoriaID,
        //                                    SegmentoID,
        //                                    SubsegmentoID,
        //                                    VentaEnMonto,
        //                                    VentaEnCantidad,
        //                                    MermaEnMonto,
        //                                    MermaEnCantidad,
        //                                    Insercion
        //                               FROM {tablaICM}";

        //    string parametros = $@" WHERE \""Fecha\"" >= '{dateStartFormatted}' ";

        //    DataTable dt = await _icmservice.ConsultarICM(tablaICM, consultaICM, modeloICM, parametros);

        //    string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

        //    return mensaje;

        //}
        //#endregion


        //#region BulkCreate como Azure Function HTTPTrigger.

        //[Function("BulkCreate_Trigger_DTOXXOTDAVENTAYMERMAPORESTRUCTURA")]
        //public async Task<HttpResponseData> BulkCreate_Trigger_DTOXXOTDAVENTAYMERMAPORESTRUCTURA([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_DTOXXOTDAVENTAYMERMAPORESTRUCTURA")] HttpRequestData req)
        //{
        //    _logger.LogInformation("Inicio de la función BulkCreate_Trigger_DTOXXOTDAVENTAYMERMAPORESTRUCTURA.");
        //    var response = req.CreateResponse();
        //    response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        //    try
        //    {
        //        string mensaje = await BulkCreate_DTOXXOTDAVENTAYMERMAPORESTRUCTURA();

        //        var result = new
        //        {
        //            message = mensaje,
        //            timestamp = DateTime.UtcNow
        //        };

        //        if (mensaje.Contains("Sin datos por insertar"))
        //        {
        //            response.StatusCode = HttpStatusCode.Accepted; // 202 Accepted
        //        }
        //        else
        //        {
        //            response.StatusCode = HttpStatusCode.OK; // 200 OK
        //        }

        //        await response.WriteStringAsync(JsonConvert.SerializeObject(result));
        //        _logger.LogInformation(mensaje);

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Ocurrió un error al procesar la solicitud: {Message}", ex.Message);
        //        response.StatusCode = HttpStatusCode.InternalServerError;
        //        await response.WriteStringAsync(ex.Message);

        //    }
        //    finally
        //    {
        //        _logger.LogInformation("Fin de la función BulkCreate_Trigger_DTOXXOTDAVENTAYMERMAPORESTRUCTURA");

        //    }

        //    return response;

        //}
        #endregion


        //#region BulkCreate como Azure Function Timer.

        ////1 vez al mes, el dia 12 una ejecución.

        //[Function("BulkCreate_Timer_DTOXXOTDAVENTAYMERMAPORESTRUCTURA_Day12")]
        //public async Task BulkCreate_Timer_DTOXXOTDAVENTAYMERMAPORESTRUCTURA_Day12([TimerTrigger("0 30 23 12 * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_DTOXXOTDAVENTAYMERMAPORESTRUCTURA_Day12.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_DTOXXOTDAVENTAYMERMAPORESTRUCTURA();
        //        _logger.LogInformation(mensaje);


        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_DTOXXOTDAVENTAYMERMAPORESTRUCTURA_Day12: {Message}", ex.Message);


        //    }


        //}

        ////1 vez al mes, el dia 13 cada 4 horas, desde las 3:30 AM hasta las 7:30 PM

        //[Function("BulkCreate_Timer_DTOXXOTDAVENTAYMERMAPORESTRUCTURA_Day13")]
        //public async Task BulkCreate_Timer_DTOXXOTDAVENTAYMERMAPORESTRUCTURA_Day13([TimerTrigger("0 30 3,7,11,15,19 13 * *")] TimerInfo myTimer)
        //{

        //    _logger.LogInformation("Inicio de la función BulkCreate_Timer_DTOXXOTDAVENTAYMERMAPORESTRUCTURA_Day13.");


        //    try
        //    {
        //        string mensaje = await BulkCreate_DTOXXOTDAVENTAYMERMAPORESTRUCTURA();
        //        _logger.LogInformation(mensaje);
        //    }
        //    catch (Exception ex)
        //    {

        //        _logger.LogError(ex, "Error al ejecutar la función BulkCreate_Timer_DTOXXOTDAVENTAYMERMAPORESTRUCTURA_Day13: {Message}", ex.Message);


        //    }


        //}
        //#endregion


    }
}
