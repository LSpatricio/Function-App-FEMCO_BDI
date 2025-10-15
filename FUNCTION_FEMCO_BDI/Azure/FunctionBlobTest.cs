using Azure.Storage.Blobs;
using FUNCTION_FEMCO_BDI.DAO;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.Azure
{
    public class FunctionBlobTest
    {
        private readonly ILogger _logger;
        
     //   private readonly BlobService _blobservice;
   //     private readonly BlobServiceClient _blobServiceClient;
        private DAO_SQL dao_sql = new DAO_SQL();

        public FunctionBlobTest(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FunctionBlobTest>();
       
        }

      

        [Function("DownloadLogsCsv")]
        public async Task<HttpResponseData> DownloadLogsCsv(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "descargar-logs-csv")] HttpRequestData req,
        FunctionContext executionContext)
        {
            _logger.LogInformation("Procesando solicitud para descargar archivo CSV de logs");

            try
            {

                
                DataTable dataTable = await dao_sql.Execute_Stored_Procedure_Datatable("sp_Get_LOG_PROCESOS");

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var noContentResponse = req.CreateResponse(System.Net.HttpStatusCode.NoContent);
                    await noContentResponse.WriteStringAsync("No hay datos para exportar.");
                    return noContentResponse;
                }

                var csvBytes = ConvertDataTableToCsvBytes(dataTable);

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/csv");
                response.Headers.Add("Content-Disposition", "attachment; filename=LOG_PROCESOS.csv");
                await response.WriteBytesAsync(csvBytes);

                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error al descargar el archivo: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error interno al descargar el archivo. {ex}");
                return errorResponse;
            }
        }

        private byte[] ConvertDataTableToCsvBytes(DataTable dataTable)
        {
            string csv_separator = ";";
            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream))
            {
                var columnNames = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                writer.WriteLine(string.Join(csv_separator, columnNames));

                foreach (DataRow row in dataTable.Rows)
                {
                    System.Collections.Generic.IEnumerable<string> fields = row.ItemArray.Select(field => EscapeCsv(field.ToString()));
                    writer.WriteLine(string.Join(csv_separator, fields));
                }

                writer.Flush();
                return memoryStream.ToArray();
            }
        }

        private string EscapeCsv(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = value.Replace("\r\n", "");
                if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                {
                    value = value.Replace("\"", "\"\"");
                    return $"\"{value}\"";
                }
            }
            return value;
        }

     
    }
}
