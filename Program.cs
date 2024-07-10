using Musializer.Models;
using Raylib_cs;

namespace Musializer
{
    internal class Program
    {
        public static bool CAPTURE_MODE = false;
        public const float WEBSOCKET_UPDATE_INTERVAL = 0f; // 100 ms
        public static async Task Main()
        {
            string[] urls = new string[] { "http://localhost:8080/", "http://localhost:8081/" };

            WebSocketServer webSocketServer = new WebSocketServer(urls[0]);
            WebServer webServer = new WebServer(urls[1]);
            AudioDeviceManager audioDeviceManager = new AudioDeviceManager();

            webServer.Start();
            webSocketServer.Start();
            
            float lastWebSocketUpdateTime = 0;
            
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.InitWindow(650, 300, "Musializer");
            Raylib.SetTargetFPS(60);
            Visualizer visualizer = new Visualizer();
            
            while (!Raylib.WindowShouldClose())
            {
                
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_M))
                {
                    CAPTURE_MODE = !CAPTURE_MODE;
                }

                if (CAPTURE_MODE)
                {
                    visualizer.Visualize();
                    if (Raylib.GetTime() >= lastWebSocketUpdateTime + WEBSOCKET_UPDATE_INTERVAL)
                    {
                        await webSocketServer.SendAudioData();
                        lastWebSocketUpdateTime = (float)Raylib.GetTime();
                    }
                }else
                    visualizer.RenderStartScreen();
            }
            visualizer.Dispose();
            webSocketServer.Dispose();
            Raylib.CloseWindow();
        }
    }
}
