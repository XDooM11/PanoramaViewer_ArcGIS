using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using System.Threading.Tasks;

namespace PanoramaViewer;
internal abstract class PanoramaToolBase : MapTool
{
    protected PanoramaToolBase()
    {
        IsSketchTool = false;
        UseSnapping = false;
    }
    protected override void OnToolMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        e.Handled = true;
        try
        {
            var view = MapView.Active;
            if (view == null) return;
            var point = view.ClientToMap(e.GetPosition(view));
            if (point == null) return;
            // Projection creates a new geometry; the map and source data remain untouched.
            var wgs = (MapPoint)GeometryEngine.Instance.Project(point, SpatialReferences.WGS84);
            if (!double.IsFinite(wgs.X) || !double.IsFinite(wgs.Y) || Math.Abs(wgs.Y) > 90 || Math.Abs(wgs.X) > 180)
                throw new InvalidOperationException("Nelze získat platné souřadnice WGS 84.");
            Open(wgs.Y, wgs.X);
        }
        catch (Exception ex)
        {
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message, "Panorama Viewer");
        }
    }
    protected abstract void Open(double latitude, double longitude);
}
internal class GoogleTool : PanoramaToolBase
{
    protected override void Open(double latitude, double longitude)
    {
        var c = CultureInfo.InvariantCulture;
        var url = $"https://www.google.com/maps/@?api=1&map_action=pano&viewpoint={latitude.ToString("F7", c)}%2C{longitude.ToString("F7", c)}";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
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
            if (string.IsNullOrWhiteSpace(KeyStore.Load())) return;
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
    protected override void OnClick() => new SettingsWindow().ShowDialog();
}
