using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Diagnostics;
using System.Numerics;

using Musializer.Models;
using Musializer;
public class Visualizer : IDisposable {
    private const int FontSize = 30;
    private readonly AudioProcessor audioProcessor;
    public Visualizer() {
        audioProcessor = new AudioProcessor();
    }

    public async void InitAsync() {
        // Your async initialization code if needed
    }

    public void Visualize() {
        BeginDrawing();
        ClearBackground(GetColor(0x101010FF));

        string message = "Sound capture is enabled...";
        int fontSize = FontSize;
        
        int textWidth = MeasureText(message, fontSize);
        int messageX = GetScreenWidth() / 2 - textWidth / 2;
        int messageY = GetScreenHeight() / 2 - fontSize * 4;
        
        DrawText(message, messageX, messageY, fontSize, Color.GREEN);
        
        // Dimensions du bouton
        int buttonWidth = 200;
        int buttonHeight = 100;
        
        int buttonX = GetScreenWidth() / 2;
        int buttonY = GetScreenHeight() / 2 - fontSize;
        
        // Dessin du bouton
        DrawButton("Setting", buttonX, buttonY, buttonWidth, buttonHeight, (string cb) => OpenURL("http://localhost:8081"));

        // Dimensions du bouton
        buttonWidth = 450;
        buttonHeight = 100;
        
        buttonX = GetScreenWidth() / 2;
        buttonY = GetScreenHeight() / 2 + buttonHeight - fontSize / 2;
        
        // Dessin du bouton
        DrawButton("Disable sound capture", buttonX, buttonY, buttonWidth, buttonHeight, (string cb) => Program.CAPTURE_MODE = false);

        EndDrawing();
    }


    public void RenderStartScreen() {
        BeginDrawing();
        ClearBackground(GetColor(0x101010FF));

        string message = "Sound capture is disabled...";
        int fontSize = FontSize;
        
        int textWidth = MeasureText(message, fontSize);
        int messageX = GetScreenWidth() / 2 - textWidth / 2;
        int messageY = GetScreenHeight() / 2 - fontSize * 4;
        
        DrawText(message, messageX, messageY, fontSize, Color.RED);
        
        // Dimensions du bouton
        int buttonWidth = 425;
        int buttonHeight = 100;
        
        int buttonX = GetScreenWidth() / 2;
        int buttonY = GetScreenHeight() / 2;
        
        // Dessin du bouton
        DrawButton("Enable sound capture", buttonX, buttonY, buttonWidth, buttonHeight, (string cb) => Program.CAPTURE_MODE = true);
        
        EndDrawing();
    }

    private void OpenURL(string url) {
        try {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        } catch (Exception ex) {
            Console.WriteLine($"Failed to open URL {url}: {ex.Message}");
            // Handle exception as needed
        }
    }
    private void DrawButton(string buttonText, int centerX, int centerY, int width, int height, Action<string> callback)
    {
        Color buttonColor = Color.GRAY;
        Color textColor = Color.WHITE;

        // Calculate the top-left corner of the button rectangle
        int posX = centerX - width / 2;
        int posY = centerY - height / 2;

        Rectangle rect = new Rectangle(posX, posY, width, height);

        // Check if the mouse is over the button
        if (CheckCollisionPointRec(GetMousePosition(), rect))
        {
            buttonColor = Color.LIGHTGRAY;
            textColor = Color.GRAY;

            if (IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
            {
                // Execute the callback function when the button is clicked
                callback?.Invoke(buttonText); // Pass button text as needed
            }
        }

        // Draw button rectangle with border
        DrawRectangleLinesEx(rect, 2, Color.DARKGRAY);
        DrawRectangleRec(rect, buttonColor);

        // Draw button text
        int textWidth = MeasureText(buttonText, FontSize);
        int textHeight = FontSize;

        DrawText(buttonText, (int)(posX + width / 2 - textWidth / 2), (int)(posY + height / 2 - textHeight / 2), FontSize, textColor);
    }

    public void Dispose() {
        audioProcessor?.Close();
    }
}
