using Azure.Storage.Blobs;
using FUNCTION_FEMCO_BDI.DAO;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.Azure
{
    public class Blob_Function
    {
        private readonly ILogger<Blob_Function> _logger;
        private readonly BlobService _blobservice;

        string KeyPrivate = Environment.GetEnvironmentVariable("PGP_PRIVATE_KEY_Base64");

        string KeyPublic = Environment.GetEnvironmentVariable("PGP_PUBLIC_KEY_Base64");

        

        public Blob_Function(ILogger<Blob_Function> logger, BlobService blobService)
        {
            _logger = logger;
            _blobservice = blobService;
        }
        /// <summary>
        /// Detecta archivo csv del contenedor sqlmi-sftp-csv-base, lo ecnripta a .pgp y lo sube al contenedor sftpicm-csv-encrypted.
        /// Cambia el metadato "Encriptado" del archivo csv a "Si".
        /// Agrega al archivo pgp el metadato "EnviadoSFTP" con valor por defecto "No".
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="name"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        [Function("UpluadBlobEncrypted")]
        public async Task UpluadBlobEncrypted([BlobTrigger("sqlmi-sftp-csv-base/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name, IDictionary<string, string> metaData)
        {
            try

            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(_blobservice.CadCone);

                if (metaData.ContainsKey("Encriptado") && metaData["Encriptado"] == "Si")
                {
                    _logger.LogInformation($"El archivo {name} ya está encriptado.");
                    return;
                }
               
                ////
                ////Se lee el contenido
                //var blobStreamReader = new StreamReader(stream);
               
                ////Se lee el contenido
                //string csvContent = await blobStreamReader.ReadToEndAsync();

                //Se crea un stream que sea el contenido encryptado
                using (MemoryStream encryptedStream = new MemoryStream())
                {


                    // Crear el cliente de Blob Service

                    string pgpKey = Encoding.UTF8.GetString(Convert.FromBase64String(KeyPublic));

                    // Convertir a MemoryStream (si necesitas trabajar con stream)
                    MemoryStream lectorStream = new MemoryStream(Encoding.UTF8.GetBytes(pgpKey));

                    PgpPublicKey publicKey = Funcionalidad_Encriptacion.ReadPublicKey(lectorStream);

                    //EncryptCsvToStream(csvContent, encryptedStream, publicKey, name);
                    await Funcionalidad_Encriptacion.EncryptCsvToStream(stream, encryptedStream, publicKey, name);
                    //Subir archivo a otro contenedor
                    BlobClient blobEncriptado = await _blobservice.UploadBlobAsync(encryptedStream, blobServiceClient, $"{name}.pgp", "sqlmi-sftp-csv-base-encripted");

                    IDictionary<string, string> metaDataBlob = await _blobservice.ObtenerMetadata(blobEncriptado);

                    metaDataBlob.Add("EnviadoSFTP", "No");

                    await blobEncriptado.SetMetadataAsync(metaDataBlob);
                }

                //Ajuste al metadato del csv origen
                BlobClient blobCSVBase = await _blobservice.ObtenerBlob(blobServiceClient, "sqlmi-sftp-csv-base", name);

                IDictionary<string, string> metaDataBlobCSVBase = await _blobservice.ObtenerMetadata(blobCSVBase);

                if (metaDataBlobCSVBase.ContainsKey("Encriptado"))
                {
                    // Si ya existe, actualiza el valor de "Encriptado"
                    metaDataBlobCSVBase["Encriptado"] = "Si";
                }
                else
                {
                    // Si no existe, agregar la clave "Encriptado" con el valor "Si"
                    metaDataBlobCSVBase.Add("Encriptado", "Si");

                }


                await blobCSVBase.SetMetadataAsync(metaDataBlobCSVBase);





            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: {Message}", ex.Message);
                // throw new Exception(ex.Message, ex);

            }


        }

        /// <summary>
        /// Detecta archivo pgp del contenedor sftpicm-csv-encrypted, lo desencripta a csv y lo sube al contenedor sftpicm-csv-decrypted.
        /// Cambia el metadato "Desencriptado" a "Si".
        /// Agrega al archivo csv el metadato "ProcesadoSQL" con valor por defecto "No".
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="name"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        [Function("UploadBlobDecrypted")]
        public async Task UploadBlobDecrypted([BlobTrigger("sftpicm-csv-encrypted/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name, IDictionary<string, string> metaData)
        {
            try

            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(_blobservice.CadCone);

                if (metaData.ContainsKey("Desencriptado") && metaData["Desencriptado"] == "Si")
                {
                    _logger.LogInformation($"El archivo {name} ya está desencriptado.");
                    return;
                }

                // stream.Position = 0;
                //Se crea un stream que sea el contenido 
                using (MemoryStream decryptedStream = new MemoryStream())
                {


                    // Crear el cliente de Blob Service

                    string pgpKey = Encoding.UTF8.GetString(Convert.FromBase64String(KeyPrivate));


                    using (MemoryStream lectorStream = new MemoryStream(Encoding.UTF8.GetBytes(pgpKey)))
                    {

                        Funcionalidad_Encriptacion.DecryptFileWithPgp(stream, decryptedStream, lectorStream);

                    }

                    //Subir archivo a otro contenedor
                    string nombreCsvDesencriptado = name.Replace(".pgp", "");

                    BlobClient blobDesencriptado = await _blobservice.UploadBlobAsync(decryptedStream, blobServiceClient, $"{nombreCsvDesencriptado}", "sftpicm-csv-decrypted");

                    IDictionary<string, string> metaDataBlob = await _blobservice.ObtenerMetadata(blobDesencriptado);

                    metaDataBlob.Add("ProcesadoSQL", "No");

                    await blobDesencriptado.SetMetadataAsync(metaDataBlob);

                }
                //Ajuste al metadato del csv origen
                BlobClient blobEncriptado = await _blobservice.ObtenerBlob(blobServiceClient, "sftpicm-csv-encrypted", name);

                IDictionary<string, string> metaDataBlobCSVBase = await _blobservice.ObtenerMetadata(blobEncriptado);

                if (metaDataBlobCSVBase.ContainsKey("Desencriptado"))
                {
                    // Si ya existe, actualiza el valor de "Encriptado"
                    metaDataBlobCSVBase["Desencriptado"] = "Si";
                }
                else
                {
                    // Si no existe, agregar la clave "Encriptado" con el valor "Si"
                    metaDataBlobCSVBase.Add("Desencriptado", "Si");

                }


                await blobEncriptado.SetMetadataAsync(metaDataBlobCSVBase);





            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: {Message}", ex.Message);
                // throw new Exception(ex.Message, ex);

            }


        }




    }
}
