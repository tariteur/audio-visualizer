using System;
using System.Collections.Generic;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;

public class AudioArray
{
    private List<Complex[]> smooth = new List<Complex[]>();
    private const int Count = 128;

    public event EventHandler<DataProcessedEventArgs> DataProcessed;

    public AudioArray()
    {
        InitializeAudioCapture();
    }

    private void InitializeAudioCapture()
    {
        var audioArrayProcessor = this;

        // Configure audio capture using WasapiLoopbackCapture
        var capture = new NAudio.Wave.WasapiLoopbackCapture();
        capture.DataAvailable += (sender, e) =>
        {
            byte[] buffer = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, buffer, e.BytesRecorded);
            audioArrayProcessor.ProcessData(buffer);
        };

        capture.RecordingStopped += (sender, e) => capture.Dispose();
        capture.StartRecording();
    }

    public void ProcessData(byte[] bufferData)
    {
        // Processing audio data
        var buffer = new WaveBuffer(bufferData);

        // Each audio frame contains 8 float values (32 bits each)
        int len = bufferData.Length / sizeof(float);
        var values = new Complex[len / 8];

        // Convert float values to Complex for Fourier transform
        for (int i = 0; i < values.Length; i++)
            values[i] = new Complex(buffer.FloatBuffer[i], 0.0);

        // Fourier transform
        Fourier.Forward(values, FourierOptions.Default);

        // Add transformed data to 'smooth' list
        smooth.Add(values);
        if (smooth.Count > 3)
            smooth.RemoveAt(0);

        // Raise event with transformed data
        OnDataProcessed(new DataProcessedEventArgs { SmoothData = smooth });
    }

    protected virtual void OnDataProcessed(DataProcessedEventArgs e)
    {
        DataProcessed?.Invoke(this, e);
    }
}

public class DataProcessedEventArgs : EventArgs
{
    public List<Complex[]> SmoothData { get; set; }
}
