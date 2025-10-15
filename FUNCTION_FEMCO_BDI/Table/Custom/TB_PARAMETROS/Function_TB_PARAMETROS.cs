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

namespace FUNCTION_FEMCO_BDI.Table.Custom.TB_PARAMETROS
{
    public class Function_TB_PARAMETROS
    {
        private readonly ILogger _logger;
        private readonly DAO_SQL _dao;
        private readonly ICMService _icmservice;

        private const string NOMBRE_TABLA = "FEMCO_Transfer.TB_PARAMETROS";

        public Function_TB_PARAMETROS(ILoggerFactory loggerFactory,DAO_SQL dao, ICMService icmService)
        {
            _logger = loggerFactory.CreateLogger<Function_TB_PARAMETROS>();
            _dao = dao;
            _icmservice = icmService;


        }

        #region BulkCreate como método.
        public async Task<string> BulkCreate_TB_PARAMETROS()
        {
            string modeloICM = Environment.GetEnvironmentVariable("ModelFemcoDev");
            string TablaICM = "FemcoTransferTbParametros";

            string ConsultaICM = @"SELECT IdParametro,
                                                Valor,
                                                ValorDT,
                                                Descripcion
                                                 FROM " + TablaICM;

            string parametros = "";
            DataTable dt = await _icmservice.ConsultarICM(TablaICM, ConsultaICM, modeloICM, parametros);

            string mensaje = await _dao.bulkInserWithtDelete(dt, NOMBRE_TABLA);

            return mensaje;

        }
        #endregion


        #region BulkCreate como Azure Function HTTPTrigger.

        [Function("BulkCreate_Trigger_TB_PARAMETROS")]
        public async Task<HttpResponseData> BulkCreate_Trigger_TB_PARAMETROS([HttpTrigger(AuthorizationLevel.Function, "post", Route = "BulkCreate_Trigger_TB_PARAMETROS")] HttpRequestData req)
        {
            _logger.LogInformation("Inicio de la función BulkCreate_Trigger_TB_PARAMETROS.");
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                string mensaje = await BulkCreate_TB_PARAMETROS();

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
                _logger.LogInformation("Fin de la función BulkCreate_Trigger_TB_PARAMETROS");

            }

            return response;

        }
        #endregion




    }
}
