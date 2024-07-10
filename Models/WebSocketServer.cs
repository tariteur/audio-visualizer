using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Musializer.Models
{
    public class AudioData
    {
        public int M { get; set; }
        public float[]? AudioArray { get; set; }
    }

    public class WebSocketServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AudioArray _audioArray;
        private readonly AudioDeviceManager _audioDeviceManager;
        private readonly List<WebSocket> _clients;
        private float[] _audioArrayData;
        public class WebSocketMessage
        {
            public string type { get; set; }
            public JsonElement data { get; set; }
        }

        public WebSocketServer(string prefix)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _cancellationTokenSource = new CancellationTokenSource();
            _audioArray = new AudioArray();
            _clients = new List<WebSocket>();
            _audioDeviceManager = new AudioDeviceManager();

            _audioArray.DataProcessed += AudioArray_DataProcessed;
        }

        public async Task Start()
        {
            try
            {
                _listener.Start();
                Console.WriteLine("WebSocket server started.");

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        await ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Start: {ex.Message}");
            }
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                Console.WriteLine("Client connected.");

                lock (_clients)
                {
                    _clients.Add(webSocket);
                }

                await HandleClientWebSocket(webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing WebSocket request: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
        }

        private void AudioArray_DataProcessed(object sender, DataProcessedEventArgs e)
        {
            double[] smoothedValues = new double[128]; // Count est défini quelque part dans votre classe
        
            // Calculer les valeurs lissées pour chaque index
            for (int index = 0; index < 128; index++)
            {
                smoothedValues[index] = Smooth(e.SmoothData, index);
            }
        
            // Convertir les valeurs lissées en tableau de float
            _audioArrayData = ConvertSmoothedValuesToFloatArray(smoothedValues);
        }
        
        private double Smooth(List<Complex[]> smoothData, int index)
        {
            double smoothedValue = 0;
            var Count = 128; // Vous devez définir Count correctement dans votre classe
        
            int start = Math.Max(index - 1, 0);
            int end = Math.Min(index + 1, Count - 1);
        
            foreach (var array in smoothData)
            {
                if (array != null && index < array.Length)
                {
                    double sum = 0;
                    for (int i = start; i <= end; i++)
                    {
                        sum += Math.Abs(array[i].Magnitude);
                    }
                    smoothedValue += sum / (end - start + 1); // Moyenne des valeurs voisines
                }
            }
        
            return smoothedValue / smoothData.Count;
        }
        
        private float[] ConvertSmoothedValuesToFloatArray(double[] smoothedValues)
        {
            float[] floatArray = new float[smoothedValues.Length];
            for (int i = 0; i < smoothedValues.Length; i++)
            {
                floatArray[i] = (float)smoothedValues[i];
            }
            return floatArray;
        }
        public async Task SendAudioData()
        {
            var audioDataJson = JsonSerializer.Serialize(_audioArrayData);
            foreach (var client in _clients)
            {
                await SendToClient(client, audioDataJson, "audioData");
            }
        }
        private async Task HandleClientWebSocket(WebSocket webSocket)
        {
            var receiveBuffer = new byte[1024];

            try
            {
                var devices = _audioDeviceManager.GetAudioDevices();
                await SendToClient(webSocket, devices, "deviceList");

                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        lock (_clients)
                        {
                            _clients.Remove(webSocket);
                        }
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocket server", CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                        Console.WriteLine($"Received from client: {receivedMessage}");
        
                        // Deserialize received message
                        var message = JsonSerializer.Deserialize<WebSocketMessage>(receivedMessage);
        
                        if (message?.type == "selectDevice" && message.data is JsonElement data)
                        {
                            string deviceName = data.GetProperty("name").GetString();
                            if (!string.IsNullOrEmpty(deviceName))
                            {
                                // Change audio device
                                ChangeAudioDevice(deviceName);
                                Console.WriteLine($"Changed audio device to: {deviceName}");
                            }
                            else
                            {
                                Console.WriteLine("Invalid device name received.");
                            }
                        }
                    }
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocketException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                webSocket.Dispose();
                Console.WriteLine("Client disconnected.");
            }
        }

        public async Task SendToClient(WebSocket client, object data, string type)
        {
            try
            {
                if (client.State == WebSocketState.Open)
                {
                    var message = new { type, data };
                    string jsonString = JsonSerializer.Serialize(message);
                    byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
                    var buffer = new ArraySegment<byte>(jsonBytes);
                    await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocketException in SendToClient: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendToClient: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
            }
        }


        public void ChangeAudioDevice(string name)
        {
            _audioDeviceManager.SetDefaultAudioDevice(name);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();

            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client.Dispose();
                }
                _clients.Clear();
            }

            _listener.Close();
        }
    }
}
