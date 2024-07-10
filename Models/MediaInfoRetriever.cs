using System;
using System.Threading.Tasks;
using Windows.Media.Control;

public static class MediaInfoRetriever {
    private static GlobalSystemMediaTransportControlsSession? _session;
    private static string? _currentArtist;
    private static string? _currentTitle;

    public static async Task Init() {
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _session = manager.GetCurrentSession();

        if (_session != null) {
            await UpdateMediaProperties();
        }
    }

    public static async Task Update() {
        if (_session != null) {
            await UpdateMediaProperties();
        }
    }

    public static string GetArtist() {
        return _currentArtist;
    }

    public static string GetTitle() {
        return _currentTitle;
    }

    private static async Task UpdateMediaProperties() {
        var mediaProperties = await _session.TryGetMediaPropertiesAsync();

        if (mediaProperties != null) {
            _currentArtist = mediaProperties.Artist;
            _currentTitle = mediaProperties.Title;
        } else {
            _currentArtist = "Unknown";
            _currentTitle = "Unknown";
        }
    }
}
