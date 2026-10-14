using System;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace PanoramaViewer;

/// <summary>
/// A transient graphic belonging to a panorama window, not to a feature layer.
/// Coordinates received from the provider are converted for display only.
/// </summary>
internal sealed class PanoramaPositionOverlay : IDisposable
{
    private readonly PanoramaProvider _provider;
    private IDisposable? _halo;
    private IDisposable? _dot;
    private MapView? _view;
    private int _version;
    private int _disposed;

    internal PanoramaPositionOverlay(PanoramaProvider provider) => _provider = provider;

    internal Task UpdateAsync(MapView view, double latitude, double longitude)
    {
        var version = Interlocked.Increment(ref _version);
        return QueuedTask.Run(() =>
        {
            if (Volatile.Read(ref _disposed) != 0 || version != Volatile.Read(ref _version)) return;
            if (!double.IsFinite(latitude) || !double.IsFinite(longitude) ||
                Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180) return;

            var spatialReference = view.Map?.SpatialReference;
            if (spatialReference == null)
                throw new InvalidOperationException("Mapa nem\u00e1 definovan\u00fd sou\u0159adnicov\u00fd syst\u00e9m.");

            var wgs84 = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
            var point = GeometryEngine.Instance.Project(wgs84, spatialReference);
            if (!ReferenceEquals(_view, view))
            {
                RemoveCore();
                _view = view;
            }

            // Separate colours when both providers are open simultaneously.
            var colour = _provider == PanoramaProvider.Mapy
                ? ColorFactory.Instance.CreateRGBColor(0, 120, 235)
                : ColorFactory.Instance.CreateRGBColor(220, 125, 0);
            var haloSymbol = SymbolFactory.Instance.ConstructPointSymbol(
                ColorFactory.Instance.CreateRGBColor(255, 255, 255),
                16, SimpleMarkerStyle.Circle).MakeSymbolReference();
            var dotSymbol = SymbolFactory.Instance.ConstructPointSymbol(
                colour, 11, SimpleMarkerStyle.Circle).MakeSymbolReference();

            if (_halo == null)
                _halo = view.AddOverlay(point, haloSymbol);
            else if (!view.UpdateOverlay(_halo, point, haloSymbol))
            {
                _halo.Dispose();
                _halo = view.AddOverlay(point, haloSymbol);
            }

            if (_dot == null)
                _dot = view.AddOverlay(point, dotSymbol);
            else if (!view.UpdateOverlay(_dot, point, dotSymbol))
            {
                _dot.Dispose();
                _dot = view.AddOverlay(point, dotSymbol);
            }
        });
    }

    internal Task ClearAsync()
    {
        var version = Interlocked.Increment(ref _version);
        return QueuedTask.Run(() =>
        {
            if (version == Volatile.Read(ref _version)) RemoveCore();
        });
    }

    private void RemoveCore()
    {
        // Called on the main CIM thread (MCT).
        try { _dot?.Dispose(); }
        finally
        {
            _dot = null;
            try { _halo?.Dispose(); }
            finally { _halo = null; _view = null; }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Interlocked.Increment(ref _version);
        _ = CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        try { await QueuedTask.Run(RemoveCore); }
        catch { /* Map/editor may already be closing; no persistent data exist. */ }
    }
}
