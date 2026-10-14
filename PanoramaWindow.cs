using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ArcGIS.Desktop.Mapping;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace PanoramaViewer;

internal sealed class PanoramaWindow : Window
{
    private readonly PanoramaProvider _provider;
    private readonly WebView2 _browser = new();
    private readonly PanoramaPositionOverlay _marker;
    private readonly TextBlock _status = new()
    {
        Text = "P\u0159ipravuji panoramu\u2026",
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(12, 7, 12, 7)
    };
    private bool _ready;
    private bool _initializing;
    private bool _closed;
    private long _requestId;
    private (double Lat, double Lon, MapView View)? _pending;

    private string PageName => _provider == PanoramaProvider.Mapy ? "panorama.html" : "google.html";
    private string LocalPage => "https://panorama.local/" + PageName;
    private string BaseTitle => _provider == PanoramaProvider.Mapy
        ? "Mapy.com Panorama \u2013 Panorama Viewer 1.1.1"
        : "Google Street View \u2013 Panorama Viewer";

    internal PanoramaWindow(PanoramaProvider provider)
    {
        _provider = provider;
        _marker = new PanoramaPositionOverlay(provider);
        Title = BaseTitle;
        Width = 1050; Height = 700; MinWidth = 550; MinHeight = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (Application.Current?.MainWindow is Window owner) Owner = owner;

        var grid = new Grid();
        if (provider != PanoramaProvider.Mapy)
        {
            // Leave the Google layout unchanged in this Mapy.com hotfix.
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(_status);
            Grid.SetRow(_browser, 1);
        }
        // For Mapy.com the browser always occupies the entire content area.
        // Status text is shown in the title, so show/hide cannot trigger resizing.
        grid.Children.Add(_browser);
        Content = grid;
        Loaded += async (_, _) => await InitializeAsync();
        Closed += (_, _) =>
        {
            _closed = true;
            _ready = false;
            _pending = null;
            _requestId++;
            _marker.Dispose();
            _browser.Dispose();
        };
    }

    private async Task InitializeAsync()
    {
        if (_initializing || _closed) return;
        _initializing = true;
        try
        {
            var directory = Path.GetDirectoryName(typeof(PanoramaWindow).Assembly.Location)!;
            var webDirectory = Path.Combine(directory, "Web");
            if (!File.Exists(Path.Combine(webDirectory, PageName)))
                throw new FileNotFoundException("V add-inu chyb\u00ed Web/" + PageName + ".");
            var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PanoramaViewer", "WebView2");
            Directory.CreateDirectory(profile);
            var environment = await CoreWebView2Environment.CreateAsync(null, profile);
            if (_closed) return;
            await _browser.EnsureCoreWebView2Async(environment);
            if (_closed) return;
            var core = _browser.CoreWebView2;
            if (_provider == PanoramaProvider.Mapy)
            {
                // Browser/page zoom is separate from the panorama's own FOV.
                // Keep page scale at 100%; ordinary panorama mouse-wheel zoom
                // and drag/navigation handlers are not intercepted.
                _browser.ZoomFactor = 1.0;
                core.Settings.IsZoomControlEnabled = false;
                core.Settings.IsPinchZoomEnabled = false;
            }
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.SetVirtualHostNameToFolderMapping("panorama.local", webDirectory,
                CoreWebView2HostResourceAccessKind.DenyCors);
            core.WebMessageReceived += OnWebMessageReceived;
            core.NavigationStarting += (_, args) =>
            {
                if (IsLocalPage(args.Uri))
                {
                    _ready = false;
                    _ = ClearMarkerAsync();
                    SetStatus("Na\u010d\u00edt\u00e1m panoramu\u2026");
                }
                else
                {
                    args.Cancel = true;
                    OpenExternalLink(args.Uri);
                }
            };
            core.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                OpenExternalLink(args.Uri);
            };
            core.NavigationCompleted += (_, args) =>
            {
                if (!args.IsSuccess && !_closed)
                {
                    _ready = false;
                    SetStatus("Nepoda\u0159ilo se na\u010d\u00edst rozhran\u00ed panoramy.");
                }
                // Readiness is signalled by JS after registering its message handler.
            };
            core.ProcessFailed += (_, _) =>
            {
                if (_closed) return;
                _ready = false;
                _ = ClearMarkerAsync();
                SetStatus("Prohl\u00ed\u017ee\u010d panoramy selhal. Zav\u0159ete a znovu otev\u0159ete toto okno.");
            };
            core.Navigate(LocalPage);
        }
        catch (Exception ex)
        {
            if (!_closed) SetStatus("Chyba WebView2: " + ex.Message);
        }
    }

    internal void ShowLocation(double latitude, double longitude, MapView view)
    {
        if (_closed) return;
        _requestId++;
        _pending = (latitude, longitude, view);
        _ = ClearMarkerAsync();
        SetStatus("Hled\u00e1m nejbli\u017e\u0161\u00ed panorama\u2026");
        if (_ready) SendLocation();
    }

    private void SendLocation()
    {
        if (_closed || !_ready || _pending is not { } location) return;
        try
        {
            var key = KeyStore.Load(_provider);
            if (string.IsNullOrWhiteSpace(key))
            {
                SetStatus("V Nastaven\u00ed zadejte API kl\u00ed\u010d pro tohoto poskytovatele.");
                return;
            }
            var payload = JsonSerializer.Serialize(new
            {
                type = "location", requestId = _requestId,
                lat = location.Lat, lon = location.Lon,
                apiKey = key, radius = 80
            });
            // No key in URLs to the local page, log messages, or the project.
            _browser.CoreWebView2.PostWebMessageAsJson(payload);
        }
        catch (Exception ex) { SetStatus("Chyba p\u0159ed\u00e1n\u00ed polohy: " + ex.Message); }
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (_closed || !IsLocalPage(args.Source)) return;
        try
        {
            using var document = JsonDocument.Parse(args.WebMessageAsJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var typeValue)) return;
            var type = typeValue.GetString();
            if (type == "ready")
            {
                _ready = true;
                SendLocation();
                return;
            }
            if (!root.TryGetProperty("requestId", out var requestValue) ||
                !requestValue.TryGetInt64(out var requestId) || requestId != _requestId) return;

            if (type == "status")
            {
                var state = root.GetProperty("state").GetString();
                if (state == "ready")
                {
                    // No persistent 'Panorama loaded' banner or instruction overlay.
                    SetStatus(string.Empty);
                }
                else
                {
                    SetStatus(root.TryGetProperty("message", out var message) ? message.GetString() : null);
                    await ClearMarkerAsync();
                }
            }
            else if (type == "position" && _pending is { } location &&
                     root.TryGetProperty("lat", out var latValue) && latValue.TryGetDouble(out var lat) &&
                     root.TryGetProperty("lon", out var lonValue) && lonValue.TryGetDouble(out var lon))
            {
                if (!double.IsFinite(lat) || !double.IsFinite(lon) || Math.Abs(lat) > 90 || Math.Abs(lon) > 180) return;
                try { await _marker.UpdateAsync(location.View, lat, lon); }
                catch
                {
                    if (!_closed && requestId == _requestId)
                        SetStatus("Panorama je otev\u0159en\u00e9, ale zna\u010dku se nepoda\u0159ilo vykreslit. " +
                                  "Otev\u0159ete 2D mapu a klikn\u011bte do n\u00ed znovu.");
                }
            }
            else if (type == "position-unavailable")
            {
                await ClearMarkerAsync();
                if (!_closed && requestId == _requestId)
                    SetStatus("Panorama nevr\u00e1tilo platnou polohu; zna\u010dka je skryt\u00e1.");
            }
        }
        catch (JsonException) { /* Ignore invalid web messages. */ }
        catch (Exception)
        {
            if (!_closed) SetStatus("Nepoda\u0159ilo se zpracovat zpr\u00e1vu z panoramy.");
        }
    }

    private async Task ClearMarkerAsync()
    {
        try { await _marker.ClearAsync(); }
        catch { /* A closed map view has no display to clear. */ }
    }

    private void SetStatus(string? message)
    {
        if (_closed) return;
        if (_provider == PanoramaProvider.Mapy)
        {
            // Updating the native title does not change the WebView viewport.
            // Success is deliberately silent; errors/loading remain visible.
            Title = string.IsNullOrWhiteSpace(message)
                ? BaseTitle
                : BaseTitle + " \u2014 " + message;
            return;
        }
        _status.Text = message ?? string.Empty;
        _status.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    private bool IsLocalPage(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        string.Equals(uri.Host, "panorama.local", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(uri.AbsolutePath, "/" + PageName, StringComparison.Ordinal);

    private static void OpenExternalLink(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) return;
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { /* External terms/help links must not close the panorama window. */ }
    }
}
