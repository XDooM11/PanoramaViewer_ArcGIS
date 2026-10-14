using System;
using System.Diagnostics;
using System.Globalization;
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
    protected PanoramaToolBase()
    {
        IsSketchTool = false;
        UseSnapping = false;
    }

    protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            e.Handled = true;
    }

    protected override async Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
    {
        try
        {
            var view = MapView.Active;
            if (view == null)
                return;

            var clientPoint = e.ClientPoint;

            var coordinates = await QueuedTask.Run(() =>
            {
                var mapPoint = view.ClientToMap(clientPoint);
                if (mapPoint == null)
                    throw new InvalidOperationException("Nepodařilo se určit místo kliknutí v mapě.");

                // Project returns new geometry; original map and layers remain unchanged.
                var wgs84Point = GeometryEngine.Instance.Project(
                    mapPoint, SpatialReferences.WGS84) as MapPoint;

                if (wgs84Point == null)
                    throw new InvalidOperationException("Nepodařilo se převést souřadnice do WGS 84.");

                return (Latitude: wgs84Point.Y, Longitude: wgs84Point.X);
            });

            if (!double.IsFinite(coordinates.Latitude) ||
                !double.IsFinite(coordinates.Longitude) ||
                Math.Abs(coordinates.Latitude) > 90 ||
                Math.Abs(coordinates.Longitude) > 180)
            {
                throw new InvalidOperationException("Nelze získat platné souřadnice WGS 84.");
            }

            // WPF windows must be opened on the UI thread.
            await Application.Current.Dispatcher.InvokeAsync(() =>
                Open(coordinates.Latitude, coordinates.Longitude));
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    ex.Message, "Panorama Viewer"));
        }
    }

    protected abstract void Open(double latitude, double longitude);
}

internal class GoogleTool : PanoramaToolBase
{
    protected override void Open(double latitude, double longitude)
    {
        var culture = CultureInfo.InvariantCulture;
        var lat = latitude.ToString("F7", culture);
        var lon = longitude.ToString("F7", culture);
        var url = $"https://www.google.com/maps/@?api=1&map_action=pano&viewpoint={lat},{lon}";

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }
}

internal class MapyTool : PanoramaToolBase
{
    private static PanoramaWindow? _window;

    protected override void Open(double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(KeyStore.Load()))
        {
            new SettingsWindow().ShowDialog();
            if (string.IsNullOrWhiteSpace(KeyStore.Load()))
                return;
        }

        if (_window == null || !_window.IsLoaded)
        {
            _window = new PanoramaWindow();
            _window.Closed += (_, _) => _window = null;
            _window.Show();
        }

        _window.Activate();
        _window.ShowLocation(latitude, longitude);
    }
}

internal class SettingsButton : Button
{
    protected override void OnClick()
    {
        new SettingsWindow().ShowDialog();
    }
}
