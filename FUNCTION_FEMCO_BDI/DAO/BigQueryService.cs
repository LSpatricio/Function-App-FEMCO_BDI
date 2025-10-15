using Google.Cloud.BigQuery.V2;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Google.Apis.Bigquery.v2.Data;
namespace FUNCTION_FEMCO_BDI.DAO
{
    public class BigQueryService
    {

        private readonly ILogger _logger;
        private BigQueryClient _bigQueryClient;
        private BigQueryResults results;


        public BigQueryService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BigQueryService>();
        }

        /// <summary>
        /// Inicializa el cliente de BigQuery utilizando las credenciales y el ID del proyecto configurados en variables de entorno.
        /// </summary>
        public void InitializeClient()
        {
            try
            {
                string credentialsJson = Environment.GetEnvironmentVariable("CredencialesQA_BigQuery");
                string projectId = Environment.GetEnvironmentVariable("BigQuery_ProjectID");


                if (string.IsNullOrEmpty(credentialsJson))
                {
                    throw new InvalidOperationException("La variable de entorno 'CredencialesQA_BigQuery' no está configurada.");
                }

                if (string.IsNullOrEmpty(projectId))
                {
                    throw new InvalidOperationException("La variable de entorno 'BigQuery_ProjectID' no está configurada.");
                }


                var credentials = GoogleCredential.FromJson(credentialsJson);
                _bigQueryClient = BigQueryClient.Create(projectId, credentials);

                _logger.LogInformation("Cliente de BigQuery inicializado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al inicializar el cliente de BigQuery: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ejecuta una consulta SQL en BigQuery y convierte los resultados en un DataTable.
        /// </summary>
        /// <param name="sqlQuery">Consulta SQL de BigQuery.</param>
        /// <returns>Resultados de la consulta como DataTable.</returns>
        public async Task<DataTable> ExecuteQuery(string sqlQuery)
        {

            try 
            {
                if (_bigQueryClient == null)
                {
                    throw new InvalidOperationException("El cliente de BigQuery no está inicializado.");
                }

                results =await _bigQueryClient.ExecuteQueryAsync(sqlQuery, parameters: null);
                _logger.LogInformation("Consulta ejecutada correctamente de BigQuery");

                return FuncionalidadBigQuery.ConvertToDataTable(results);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al ejecutar la consulta: {ex.Message}");
                throw;
            }
        }



        public async Task<BigQueryResults> ExecuteQueryBigQueryResults(string sqlQuery)
        {

            try
            {
                if (_bigQueryClient == null)
                {
                    throw new InvalidOperationException("El cliente de BigQuery no está inicializado.");
                }

                results = await _bigQueryClient.ExecuteQueryAsync(sqlQuery, parameters: null);
                _logger.LogInformation("Consulta ejecutada correctamente de BigQuery");
              

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al ejecutar la consulta: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Libera los recursos utilizados por BigQueryClient.
        /// </summary>
        public void Dispose()
        {
            if (_bigQueryClient != null)
            {
                _bigQueryClient.Dispose();
                _bigQueryClient = null;
                _logger.LogInformation("Cliente de BigQuery liberado.");
            }
        }

    }
}
