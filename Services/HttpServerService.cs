using System;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using System.Net;
using System.Text.Json;
using PipeWorkshopApp.Models;

namespace PipeWorkshopApp.Services
{
    public class HttpServerService
    {
        private WebServer _server;

        public async Task StartServer(string ipAddress, int port)
        {
            string url = $"http://{ipAddress}:{port}/";
            _server = CreateWebServer(url);

            try
            {
                await _server.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске сервера: {ex.Message}");
            }
        }

        public void StopServer()
        {
            _server?.Dispose();
        }

        private WebServer CreateWebServer(string url)
        {
            var server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new ActionModule("/", HttpVerbs.Get, async context =>
                {
                    await context.SendStringAsync("Сервер работает!", "text/plain", Encoding.UTF8);
                }))
                .WithModule(new ActionModule("/marking", HttpVerbs.Post, async context =>
                {
                    using var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                    string body = await reader.ReadToEndAsync();

                    MarkingData markingData = null;
                    try
                    {
                        markingData = JsonSerializer.Deserialize<MarkingData>(body);
                        ProcessMarkingData(markingData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка обработки данных маркировки: {ex.Message}");
                    }

                    context.Response.StatusCode = 200;
                    context.Response.ContentLength64 = 0;
                    context.Response.OutputStream.Close();
                }));

            return server;
        }

        public event EventHandler<MarkingData> MarkingDataReceived;

        private void ProcessMarkingData(MarkingData markingData)
        {
            MarkingDataReceived?.Invoke(this, markingData);
        }
    }
}
