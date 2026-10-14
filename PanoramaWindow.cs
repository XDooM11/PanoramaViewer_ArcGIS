using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
namespace PanoramaViewer;
internal sealed class PanoramaWindow : Window
{
    private readonly WebView2 _browser = new();
    private readonly TextBlock _status = new() { Text = "Připravuji panoramu…", Margin = new Thickness(12, 6, 12, 6) };
    private bool _ready;
    private (double lat, double lon)? _pending;
    public PanoramaWindow()
    {
        Title = "Mapy.com Panorama – Panorama Viewer";
        Width = 1050; Height = 700; MinWidth = 550; MinHeight = 350;
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(_status);
        Grid.SetRow(_browser, 1);
        grid.Children.Add(_browser);
        Content = grid;
        Loaded += async (_, _) => await InitializeAsync();
    }
    private async System.Threading.Tasks.Task InitializeAsync()
    {
        try
        {
            var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PanoramaViewer", "WebView2");
            Directory.CreateDirectory(profile);
            var env = await CoreWebView2Environment.CreateAsync(null, profile);
            await _browser.EnsureCoreWebView2Async(env);
            _browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _browser.CoreWebView2.WebMessageReceived += (_, args) =>
            {
                try
                {
                    using var msg = JsonDocument.Parse(args.WebMessageAsJson);
                    var root = msg.RootElement;
                    if (root.TryGetProperty("status", out var s))
                        Dispatcher.Invoke(() => _status.Text = s.GetString() ?? "");
                }
                catch { }
            };
            var webDir = Path.Combine(Path.GetDirectoryName(typeof(PanoramaWindow).Assembly.Location)!, "Web");
            _browser.CoreWebView2.SetVirtualHostNameToFolderMapping("panorama.local", webDir, CoreWebView2HostResourceAccessKind.DenyCors);
            _browser.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                _ready = args.IsSuccess;
                if (!_ready) _status.Text = "Nepodařilo se načíst webové rozhraní panoramy.";
                else if (_pending is { } p) _ = SendAsync(p.lat, p.lon);
            };
            _browser.CoreWebView2.Navigate("https://panorama.local/panorama.html");
        }
        catch (Exception ex) { _status.Text = "Chyba WebView2: " + ex.Message; }
    }
    public void ShowLocation(double latitude, double longitude)
    {
        _pending = (latitude, longitude);
        _status.Text = string.Format(CultureInfo.InvariantCulture, "Vybrané místo: {0:F6}° N, {1:F6}° E", latitude, longitude);
        if (_ready) _ = SendAsync(latitude, longitude);
    }
    private async System.Threading.Tasks.Task SendAsync(double latitude, double longitude)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { type = "location", lat = latitude, lon = longitude, apiKey = KeyStore.Load(), radius = 80 });
            _browser.CoreWebView2.PostWebMessageAsJson(payload);
            await System.Threading.Tasks.Task.CompletedTask;
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }
}
