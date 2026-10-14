using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace PanoramaViewer;

// Keep the existing map-tool IDs so other add-in components do not change.
internal static class PanoramaToolIds
{
    internal const string Google = "PanoramaViewer_GoogleTool";
    internal const string Mapy = "PanoramaViewer_MapyTool";
    internal const string Explore = "esri_mapping_exploreTool";
}

// A regular ribbon Button receives a second click even when its map tool is
// already active. The actual map tools remain registered in Config.daml.
internal abstract class PanoramaToggleButtonBase : Button
{
    // Only user clicks enter this gate. No timer, map redraw or API call is added.
    // Serialize activation: a rapid second click is evaluated AFTER the first
    // activation completes, and therefore returns the map to Explore.
    private static readonly SemaphoreSlim ToolChangeGate = new(1, 1);

    protected abstract string MapToolId { get; }

    protected override async void OnClick()
    {
        var clickedView = MapView.Active;
        if (clickedView == null) return;

        await ToolChangeGate.WaitAsync();
        try
        {
            // Do not apply a queued button click to another map view.
            if (!ReferenceEquals(clickedView, MapView.Active)) return;

            var target = string.Equals(
                FrameworkApplication.CurrentTool, MapToolId, StringComparison.Ordinal)
                ? PanoramaToolIds.Explore
                : MapToolId;

            // This changes only the active map tool. It does NOT close either
            // panorama window, clear overlays, change keys, or reload panoramas.
            await FrameworkApplication.SetCurrentToolAsync(target);
        }
        catch (Exception ex)
        {
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                "Nepoda\u0159ilo se zm\u011bnit aktivn\u00ed n\u00e1stroj: " + ex.Message,
                "Panorama Viewer");
        }
        finally
        {
            ToolChangeGate.Release();
            UpdateCheckedState();
        }
    }

    protected override void OnUpdate()
    {
        // Use Pro's existing control updates, not an extra event subscription
        // or background timer. This also reflects selection of Explore elsewhere.
        UpdateCheckedState();
    }

    private void UpdateCheckedState()
    {
        var isCurrent = MapView.Active != null && string.Equals(
            FrameworkApplication.CurrentTool, MapToolId, StringComparison.Ordinal);
        if (IsChecked != isCurrent) IsChecked = isCurrent;
    }
}

internal sealed class GoogleToggleButton : PanoramaToggleButtonBase
{
    protected override string MapToolId => PanoramaToolIds.Google;
}

internal sealed class MapyToggleButton : PanoramaToggleButtonBase
{
    protected override string MapToolId => PanoramaToolIds.Mapy;
}

internal abstract class PanoramaToolBase : MapTool
{
    private int _clickVersion;

    protected abstract string MapToolId { get; }

    protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
    {
        // Invalidate an unfinished map click when the user leaves the tool.
        // The existing panorama windows and their graphics remain untouched.
        Interlocked.Increment(ref _clickVersion);
        return base.OnToolDeactivateAsync(hasMapViewChanged);
    }

    private bool IsCurrentRequest(int version, MapView view) =>
        version == Volatile.Read(ref _clickVersion) &&
        ReferenceEquals(view, MapView.Active) &&
        string.Equals(FrameworkApplication.CurrentTool, MapToolId, StringComparison.Ordinal);

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
            if (!IsCurrentRequest(version, view)) return;
            if (!double.IsFinite(coordinates.Latitude) || !double.IsFinite(coordinates.Longitude) ||
                Math.Abs(coordinates.Latitude) > 90 || Math.Abs(coordinates.Longitude) > 180)
                throw new InvalidOperationException("Sou\u0159adnice WGS 84 nejsou platn\u00e9.");

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Recheck after dispatch as the user may have deselected the
                // tool while coordinate conversion was running on the MCT.
                if (IsCurrentRequest(version, view))
                    Open(coordinates.Latitude, coordinates.Longitude, view);
            });
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (version == Volatile.Read(ref _clickVersion) && string.Equals(
                    FrameworkApplication.CurrentTool, MapToolId, StringComparison.Ordinal))
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(ex.Message, "Panorama Viewer");
            });
        }
    }

    protected abstract void Open(double latitude, double longitude, MapView view);
}

internal class GoogleTool : PanoramaToolBase
{
    protected override string MapToolId => PanoramaToolIds.Google;

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
    protected override string MapToolId => PanoramaToolIds.Mapy;

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
