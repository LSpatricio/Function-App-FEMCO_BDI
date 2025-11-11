using FUNCTION_FEMCO_BDI.DAO;
using FUNCTION_FEMCO_BDI.NewFolder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System;
using FUNCTION_FEMCO_BDI.Funcionalidades;
using Azure.Storage.Blobs;

namespace FUNCTION_FEMCO_BDI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FunctionsDebugger.Enable();

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();
                    services.AddSingleton<HttpClient>(provider =>
                    {
                        var handler = new HttpClientHandler
                        {
                            UseCookies = true, // Activa o desactiva el uso de cookies
                            MaxConnectionsPerServer = 100 // Máximo número de conexiones por servidor
                        };

                        var httpClient = new HttpClient(handler)
                        {
                            Timeout = TimeSpan.FromSeconds(900) // Tiempo de espera para cada solicitud
                        };

                        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                        //httpClient.DefaultRequestHeaders.Add("Model", Environment.GetEnvironmentVariable("ModelName"));
                        httpClient.DefaultRequestHeaders.Add("Authorization", Environment.GetEnvironmentVariable("Bearer"));


                        return httpClient;
                    });
                    services.AddScoped<ICMService>();
                    services.AddScoped<DAO_SQL>();
                    services.AddScoped<FuncionalidadSQL>();
                    services.AddScoped<FuncionalidadICM>();
                    services.AddScoped<BlobService>();
                    //services.AddSingleton(provider =>
                    //{
                     //   var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                      //  return new BlobServiceClient(connectionString);
                   // });


                })
                .Build();

            host.Run();
        }
    }
}