using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Musializer.Models;
namespace Musializer.Models
{
    public class WebServer
    {
        private readonly string url;
        private readonly HttpListener listener;
        private readonly AudioProcessor audioProcessor;
        private readonly AudioDeviceManager audioDeviceManager;


        public WebServer(string prefixe)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("HTTP Listener n'est pas supporté sur ce système.");
            }

            listener = new HttpListener();
            listener.Prefixes.Add(prefixe);
            url = prefixe;

            audioProcessor = new AudioProcessor();
            audioDeviceManager = new AudioDeviceManager();
        }

        public void Start()
        {
            listener.Start();
            Console.WriteLine($"Serveur web démarré sur {url}");

            ThreadPool.QueueUserWorkItem(async (_) =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        HttpListenerContext context = await listener.GetContextAsync();
                        _ = ProcessRequestAsync(context);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur: {ex.Message}");
                }
            });
        }

        public void Stop()
        {
            listener.Stop();
            listener.Close();
            Console.WriteLine("Serveur web arrêté.");
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Récupérer le chemin demandé par le client
            string requestUrl = request.RawUrl;

            try
            {
                // if (!string.IsNullOrEmpty(requestUrl) && requestUrl.StartsWith("/api/getOutputDevices"))
                // {                    
                //     Console.WriteLine($"{response}");
                //     GetOutputDevices(response);
                // }
                // else if (!string.IsNullOrEmpty(requestUrl) && requestUrl.StartsWith("/api/SelectOutputDevice"))
                // {
                //     await HandleSelectOutputDeviceRequestAsync(request, response);
                // }
                // else
                // {
                    ServeStaticFiles(requestUrl, response);
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de traitement de la requête : {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Close();
            }
        }

        private void ServeStaticFiles(string requestUrl, HttpListenerResponse response)
        {
            string filePath = GetFilePathFromUrl(requestUrl);

            if (filePath != null && File.Exists(filePath))
            {
                ServeFile(filePath, response);
            }
            else
            {
                SendNotFoundResponse(response);
            }
        }

        private string GetFilePathFromUrl(string requestUrl)
        {
            return requestUrl switch
            {
                "/" => Path.Combine(Environment.CurrentDirectory, "src", "Main", "index.html"),
                "/main-script.js" => Path.Combine(Environment.CurrentDirectory, "src", "Main", "script.js"),
                "/visualizer" => Path.Combine(Environment.CurrentDirectory, "src", "Visualizer", "index.html"),
                "/visualizer-script.js" => Path.Combine(Environment.CurrentDirectory, "src", "Visualizer", "script.js"),
                "/visualizer_2" => Path.Combine(Environment.CurrentDirectory, "src", "Visualizer_2", "index.html"),
                _ => null
            };
        }

        private void ServeFile(string filePath, HttpListenerResponse response)
        {
            string contentType = GetContentType(filePath);

            byte[] buffer = File.ReadAllBytes(filePath);

            response.ContentType = contentType;
            response.ContentLength64 = buffer.Length;

            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath) switch
            {
                ".html" => "text/html; charset=UTF-8",
                ".js" => "text/javascript; charset=UTF-8",
                _ => "application/octet-stream"
            };
        }

        private void SendNotFoundResponse(HttpListenerResponse response)
        {
            string responseString = "<html><body><h1>404 - Fichier non trouvé</h1></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.ContentType = "text/html; charset=UTF-8";
            response.ContentLength64 = buffer.Length;

            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        // private async Task HandleSelectOutputDeviceRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        // {
        //     try
        //     {
        //         using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        //         {
        //             var content = await reader.ReadToEndAsync();
        //             var data = JsonSerializer.Deserialize<JsonElement>(content);
                    
        //             string? name = data.GetProperty("name").GetString(); // Use nullable reference type
        //             if (name != null)
        //             {
        //                 audioDeviceManager.SetDefaultAudioDevice(name);
        //                 Console.WriteLine("name: "+name);
        //                 audioProcessor.ChangeAudioDevice(name);

        //                 using (var writer = new StreamWriter(response.OutputStream))
        //                 {
        //                     await writer.WriteAsync("Périphérique sélectionné avec succès");
        //                 }
        //             }
        //             else
        //             {
        //                 Console.WriteLine("Name is null or could not be retrieved.");
        //             }

                    
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine("Erreur: " + ex.Message);
        //         response.StatusCode = (int)HttpStatusCode.InternalServerError;
        //         using (var writer = new StreamWriter(response.OutputStream))
        //         {
        //             await writer.WriteAsync("Erreur lors de la sélection du périphérique");
        //         }
        //     }
        // }

        // private void GetOutputDevices(HttpListenerResponse response)
        // {        
        //     string[] devices = audioDeviceManager.GetAudioDevices();
        //     string jsonResponse = JsonSerializer.Serialize(devices);
        //     Console.WriteLine(audioDeviceManager);

        //     // Écrire la réponse JSON
        //     WriteResponse(response, jsonResponse, "application/json");
        // }


        // private void WriteResponse(HttpListenerResponse response, string content, string contentType = "text/plain; charset=UTF-8")
        // {
        //     byte[] buffer = Encoding.UTF8.GetBytes(content);

        //     response.ContentType = contentType;
        //     response.ContentLength64 = buffer.Length;

        //     response.OutputStream.Write(buffer, 0, buffer.Length);
        //     response.Close();
        // }

        // private async Task WriteResponseAsync(HttpListenerResponse response, string content, string contentType = "text/plain; charset=UTF-8")
        // {
        //     byte[] buffer = Encoding.UTF8.GetBytes(content);

        //     response.ContentType = contentType;
        //     response.ContentLength64 = buffer.Length;

        //     await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        //     response.Close();
        // }
    }

    internal class DeviceSelectionData
    {
        public string? name { get; set; }
    }
}
