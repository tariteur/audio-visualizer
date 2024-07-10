using System;
using System.Collections.Generic;
using System.Linq;
using AudioSwitcher.AudioApi.CoreAudio;

public class AudioDeviceManager
{
    private List<CoreAudioDevice> _audioDevices;

    public AudioDeviceManager()
    {
        _audioDevices = new List<CoreAudioDevice>();

        LoadAudioDevices();
    }

    private void LoadAudioDevices()
    {
        // Create an instance of CoreAudioController
        var controller = new CoreAudioController();

        // Get all active playback devices
        var devices = controller.GetPlaybackDevices();

        // Populate _audioDevices with CoreAudioDevice instances
        _audioDevices.AddRange(devices);
    }

    public List<string> GetAudioDeviceNames()
    {
        return _audioDevices.Select(device => device.FullName).ToList();
    }

    public CoreAudioDevice GetAudioDeviceByName(string name)
    {
        // Find the device by name (case-insensitive comparison)
        return _audioDevices.FirstOrDefault(d => d.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    public void SetDefaultAudioDevice(string fullName)
    {
        if (fullName != null)
        {
            // Create an instance of CoreAudioController
            var controller = new CoreAudioController();

            // Get all active playback devices
            var devices = controller.GetPlaybackDevices();

            // Find the device by full name (case-insensitive comparison)
            var device = devices.FirstOrDefault(d => d.FullName.Contains(fullName, StringComparison.OrdinalIgnoreCase));

            if (device != null)
            {
                // Set the default playback device
                controller.DefaultPlaybackDevice = device;
            }
        }
    }

    public string[] GetAudioDevices()
    {
        return GetAudioDeviceNames().ToArray();
    }
}
