using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace PanoramaViewer;

internal abstract class PanoramaToolBase : MapTool
{
    private int _clickVersion;

    protected PanoramaToolBase()
    {
        IsSketchTool = false;
        UseSnapping = false;
    }

    protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) e.Handled = true;
    }

    protected override async Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        var version = Interlocked.Increment(ref _clickVersion);
        try
        {
            var view = MapView.Active;
            if (view == null) return;
            var clientPoint = e.ClientPoint;
            var coordinates = await QueuedTask.Run(() =>
            {
                var point = view.ClientToMap(clientPoint);
                if (point == null || point.IsEmpty || point.SpatialReference == null)
                    throw new InvalidOperationException("Nelze ur\u010dit polohu kliknut\u00ed v map\u011b.");
                // Project returns new geometry; no map or layer is modified.
                var wgs84 = GeometryEngine.Instance.Project(point, SpatialReferences.WGS84) as MapPoint;
                if (wgs84 == null || wgs84.IsEmpty)
                    throw new InvalidOperationException("P\u0159evod sou\u0159adnic do WGS 84 se nezda\u0159il.");
                return (Latitude: wgs84.Y, Longitude: wgs84.X);
            });
            if (version != Volatile.Read(ref _clickVersion)) return;
            if (!double.IsFinite(coordinates.Latitude) || !double.IsFinite(coordinates.Longitude) ||
                Math.Abs(coordinates.Latitude) > 90 || Math.Abs(coordinates.Longitude) > 180)
                throw new InvalidOperationException("Sou\u0159adnice WGS 84 nejsou platn\u00e9.");

            await Application.Current.Dispatcher.InvokeAsync(() =>
                Open(coordinates.Latitude, coordinates.Longitude, view));
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message, "Panorama Viewer"));
        }
    }

    protected abstract void Open(double latitude, double longitude, MapView view);
}

internal class GoogleTool : PanoramaToolBase
{
    protected override void Open(double latitude, double longitude, MapView view)
    {
        if (!string.IsNullOrWhiteSpace(KeyStore.Load(PanoramaProvider.Google)))
        {
            PanoramaWindows.Show(PanoramaProvider.Google, latitude, longitude, view);
            return;
        }

        // Preserve the working, key-free Maps URL option.
        // A browser link cannot send live panorama position events to ArcGIS Pro.
        PanoramaWindows.Close(PanoramaProvider.Google);
        var lat = latitude.ToString("F7", CultureInfo.InvariantCulture);
        var lon = longitude.ToString("F7", CultureInfo.InvariantCulture);
        var url = $"https://www.google.com/maps/@?api=1&map_action=pano&viewpoint={lat},{lon}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}

internal class MapyTool : PanoramaToolBase
{
    protected override void Open(double latitude, double longitude, MapView view)
    {
        if (string.IsNullOrWhiteSpace(KeyStore.Load()))
        {
            new SettingsWindow().ShowDialog();
            if (string.IsNullOrWhiteSpace(KeyStore.Load())) return;
        }
        PanoramaWindows.Show(PanoramaProvider.Mapy, latitude, longitude, view);
    }
}

internal static class PanoramaWindows
{
    private static PanoramaWindow? _mapy;
    private static PanoramaWindow? _google;

    internal static void Show(PanoramaProvider provider, double latitude, double longitude, MapView view)
    {
        var window = provider == PanoramaProvider.Mapy ? _mapy : _google;
        if (window == null)
        {
            window = new PanoramaWindow(provider);
            var created = window;
            if (provider == PanoramaProvider.Mapy) _mapy = created;
            else _google = created;
            created.Closed += (_, _) =>
            {
                if (ReferenceEquals(_mapy, created)) _mapy = null;
                if (ReferenceEquals(_google, created)) _google = null;
            };
            created.Show();
        }
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.ShowLocation(latitude, longitude, view);
        window.Activate();
    }

    internal static void Close(PanoramaProvider provider)
    {
        if (provider == PanoramaProvider.Mapy) _mapy?.Close();
        else _google?.Close();
    }
}

internal class SettingsButton : Button
{
    protected override void OnClick() => new SettingsWindow().ShowDialog();
}
