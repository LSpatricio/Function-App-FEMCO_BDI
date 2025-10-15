using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.DAO
{
    public class BlobService
    {
        public string CadCone => Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        public async Task<BlobClient> ObtenerBlob(BlobServiceClient blobServiceClient, string containerName, string blobName)
        {
            //acceder al contenedor

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (await containerClient.ExistsAsync())
            {
                //acceder al blob
                BlobClient blob = containerClient.GetBlobClient(blobName);

                if (await blob.ExistsAsync())
                {
                    return blob;
                }
                {
                    throw new ArgumentException("No existe blob");
                }
            }
            else
            {
                throw new ArgumentException("No existe contenedor");
            }

        }


        public async Task<IDictionary<string, string>> ObtenerMetadata(BlobClient blob)
        {
            BlobProperties propiedades = await blob.GetPropertiesAsync();

            IDictionary<string, string> metaData = propiedades.Metadata;

            return metaData;

        }


        public async Task<BlobClient> UploadBlobAsync(Stream encryptedStream, BlobServiceClient blobServiceClient, string blobName, string containerName)
        {
            // Crear el cliente de Blob Service
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Asegurarse de que el contenedor exista
            await containerClient.CreateIfNotExistsAsync();

            // Subir el archivo encriptado
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            encryptedStream.Position = 0; // Asegurarse de que el stream esté al inicio
            await blobClient.UploadAsync(encryptedStream, overwrite: true);

            return blobClient;
        }





    }
}
