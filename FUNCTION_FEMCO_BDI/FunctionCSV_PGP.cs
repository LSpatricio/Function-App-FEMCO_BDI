using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using System.IO;
using System.Net;
using CsvHelper;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using System.Globalization;

namespace FUNCTION_FEMCO_BDI
{
    public class FunctionCSV_PGP
    {
        private readonly ILogger _logger;

        public FunctionCSV_PGP(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FunctionCSV_PGP>();
        }


        [Function("GenerateEncryptedCsv_test")]
        public async Task<HttpResponseData> GenerateEncryptedCsv_test(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GenerateEncryptedCsv_test")] HttpRequestData req)
        {
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json");

            try
            {
                _logger.LogInformation($"Function executed at: {DateTime.Now}");

                //string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                //string containerName = "encrypted-files";

                //Ruta donde estará el CSV
                string csvFilePath = Path.Combine(Path.GetTempPath(), "data.csv");
                _logger.LogInformation(csvFilePath);
                //Crear CSV a partir de datatable
                CreateCsvFromDataTable(CreateSampleDataTable(), csvFilePath);
                _logger.LogInformation($"CSV creado en: {csvFilePath}");

                //encriptar archivo y meterlo en una ruta
                string encryptedFilePath = Path.Combine(Path.GetTempPath(), "data.csv.pgp");
                //string publicKeyPath = Path.Combine(Path.GetTempPath(), "0xD5DCFD39-pub.asc");

                string baseDirectory = @"C:\Users\lpatricio\Documents\FUNCTION_FEMCO_BDI\FUNCTION_FEMCO_BDI";
                _logger.LogInformation($"{baseDirectory}");

                string publicKeyPath = Path.Combine(baseDirectory, @"Key", "ClavePng.asc");
                _logger.LogInformation($"{publicKeyPath}");

                //await DownloadPublicKeyFromBlobStorage(storageConnectionString, containerName, "public_key.asc", publicKeyPath);

                EncryptFileWithPgp(csvFilePath, encryptedFilePath, publicKeyPath);
                _logger.LogInformation($"Archivo encriptado en: {encryptedFilePath}");

                //await UploadFileToBlobStorage(storageConnectionString, containerName, encryptedFilePath);
                _logger.LogInformation($"Archivo subido a Azure Blob Storage");

                // 📌 Crear respuesta JSON con detalles
                var result = new
                {
                    message = "Archivo CSV encriptado y subido correctamente.",
                    //blobUrl = $"https://{storageConnectionString}.blob.core.windows.net/{containerName}/data.csv.pgp",

                    timestamp = DateTime.UtcNow
                };

                await response.WriteStringAsync(JsonConvert.SerializeObject(result));
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");

                var errorResult = new
                {
                    message = "Error al procesar la solicitud",
                    error = ex.Message
                };

                await response.WriteStringAsync(JsonConvert.SerializeObject(errorResult));
                response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return response;
        }

        #region Datos prueba
        public static DataTable CreateSampleDataTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Nombre", typeof(string));
            table.Columns.Add("Email", typeof(string));

            table.Rows.Add(1, "Alice", "alice@example.com");
            table.Rows.Add(2, "Bob", "bob@example.com");

            return table;
        }
        #endregion


        [Function("GenerateEncryptedCsv")]
        public async Task<HttpResponseData> GenerateEncryptedCsv(
              [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GenerateEncryptedCsv")] HttpRequestData req)
        {
            var response = req.CreateResponse();
            response.Headers.Add("Content-Type", "application/json");

            try
            {
                _logger.LogInformation($"Function executed at: {DateTime.Now}");

                string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                string containerName = "encrypted-files";

                string csvFilePath = Path.Combine(Path.GetTempPath(), "data.csv");
                CreateCsvFromDataTable(CreateSampleDataTable(), csvFilePath);
                _logger.LogInformation($"CSV creado en: {csvFilePath}");

                string encryptedFilePath = Path.Combine(Path.GetTempPath(), "data.csv.pgp");
                string publicKeyPath = Path.Combine(Path.GetTempPath(), "public_key.asc");

             //   await DownloadPublicKeyFromBlobStorage(storageConnectionString, containerName, "public_key.asc", publicKeyPath);

                EncryptFileWithPgp(csvFilePath, encryptedFilePath, publicKeyPath);
                _logger.LogInformation($"Archivo encriptado en: {encryptedFilePath}");

              //  await UploadFileToBlobStorage(storageConnectionString, containerName, encryptedFilePath);
                _logger.LogInformation($"Archivo subido a Azure Blob Storage");

                // 📌 Crear respuesta JSON con detalles
                var result = new
                {
                    message = "Archivo CSV encriptado y subido correctamente.",
                    blobUrl = $"https://{storageConnectionString}.blob.core.windows.net/{containerName}/data.csv.pgp",
                    timestamp = DateTime.UtcNow
                };

                await response.WriteStringAsync(JsonConvert.SerializeObject(result));
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");

                var errorResult = new
                {
                    message = "Error al procesar la solicitud",
                    error = ex.Message
                };

                await response.WriteStringAsync(JsonConvert.SerializeObject(errorResult));
                response.StatusCode = HttpStatusCode.InternalServerError;
            }

            return response;
        }

    

        public static void CreateCsvFromDataTable(DataTable dataTable, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(dataTable.AsEnumerable().Select(row =>
                    new
                    {
                        ID = row.Field<int>("ID"),
                        Nombre = row.Field<string>("Nombre"),
                        Email = row.Field<string>("Email")
                    }));
            }
        }

        //private static async Task DownloadPublicKeyFromBlobStorage(string storageConnectionString, string containerName, string blobName, string destinationPath)
        //{
        //    BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);
        //    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        //    BlobClient blobClient = containerClient.GetBlobClient(blobName);

        //    if (await blobClient.ExistsAsync())
        //    {
        //        await blobClient.DownloadToAsync(destinationPath);
        //    }
        //}

        public static void EncryptFileWithPgp(string inputFilePath, string outputFilePath, string publicKeyPath)
        {
            using (Stream publicKeyStream = File.OpenRead(publicKeyPath))
            using (Stream inputFileStream = File.OpenRead(inputFilePath))
            using (Stream outputFileStream = File.Create(outputFilePath))
            {
                PgpPublicKey publicKey = ReadPublicKey(publicKeyStream);
                EncryptFile(inputFileStream, outputFileStream, publicKey);
            }
        }

        private static PgpPublicKey ReadPublicKey(Stream inputStream)
        {
            PgpPublicKeyRingBundle pgpPub = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(inputStream));
            foreach (PgpPublicKeyRing keyRing in pgpPub.GetKeyRings())
            {
                foreach (PgpPublicKey key in keyRing.GetPublicKeys())
                {
                    if (key.IsEncryptionKey) return key;
                }
            }
            throw new ArgumentException("No encryption key found in public key ring.");
        }

        private static void EncryptFile(Stream inputStream, Stream outputStream, PgpPublicKey publicKey)
        {
            using (MemoryStream bOut = new MemoryStream())
            {
                PgpCompressedDataGenerator comData = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
                using (Stream cos = comData.Open(bOut))
                {
                    PgpLiteralDataGenerator lData = new PgpLiteralDataGenerator();
                    using (Stream pOut = lData.Open(cos, PgpLiteralData.Binary, "data.csv", DateTime.UtcNow, new byte[4096]))
                    {
                        inputStream.CopyTo(pOut);
                    }
                }
                comData.Close();

                PgpEncryptedDataGenerator encGen = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, true, new SecureRandom());
                encGen.AddMethod(publicKey);
                using (Stream encOut = encGen.Open(outputStream, bOut.Length))
                {
                    bOut.Position = 0;
                    bOut.CopyTo(encOut);
                }
            }
        }

        //private static async Task UploadFileToBlobStorage(string storageConnectionString, string containerName, string filePath)
        //{
        //    BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);
        //    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        //    await containerClient.CreateIfNotExistsAsync();

        //    string blobName = Path.GetFileName(filePath);
        //    BlobClient blobClient = containerClient.GetBlobClient(blobName);

        //    using (var fileStream = File.OpenRead(filePath))
        //    {
        //        await blobClient.UploadAsync(fileStream, true);
        //    }
        //}

    }
}
