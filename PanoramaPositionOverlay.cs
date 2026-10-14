using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Events;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;

namespace PanoramaViewer;

/// <summary>
/// Transient 2D point + view sector. No features, layers, map navigation or
/// provider requests are created. All graphic operations run on the MCT.
/// </summary>
internal sealed class PanoramaPositionOverlay : IDisposable
{
    private readonly PanoramaProvider _provider;
    private readonly object _gate = new();
    private readonly SubscriptionToken _cameraSubscription;
    private readonly SubscriptionToken _activeSubscription;
    private State? _latest;
    private bool _disposed;
    private bool _suspended;
    private bool _activeView;
    private bool _warningRaised;
    private bool _faulted;
    private long _mapRevision;
    private MapView? _renderView;
    private IDisposable? _dot, _sector, _axis;
    private CIMSymbolReference? _dotSymbol, _sectorSymbol, _axisSymbol;
    private Basis? _basis;
    private long _screenRevision = -1;
    private double _screenEX, _screenEY, _screenNX, _screenNY;
    private State? _drawn;
    private double _drawnRadius;

    private sealed record State(MapView View, double Lat, double Lon, double? Heading, double? Fov);
    private sealed record Basis(SpatialReference SR, MapPoint Center, MapPoint East, MapPoint North,
        double EX, double EY, double NX, double NY, double Lat, double Lon);

    // The window dispatches this low-frequency notification onto its UI thread.
    internal event Action<string>? Warning;

    internal PanoramaPositionOverlay(PanoramaProvider provider)
    {
        _provider = provider;
        _cameraSubscription = MapViewCameraChangedEvent.Subscribe(OnCameraChanged);
        _activeSubscription = ActiveMapViewChangedEvent.Subscribe(OnActiveViewChanged);
    }

    internal void UpdateCamera(MapView view, double latitude, double longitude,
        double? heading, double? fov)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) ||
            Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180) return;
        heading = heading is double h && double.IsFinite(h) ? ((h % 360) + 360) % 360 : null;
        fov = fov is double f && double.IsFinite(f) && f > 0 ? Math.Clamp(f, 1, 180) : null;
        bool schedule;
        lock (_gate)
        {
            if (_disposed) return;
            var next = new State(view, latitude, longitude, heading, fov);
            if (next == _latest) return;
            if (!ReferenceEquals(_latest?.View, view)) { _mapRevision++; _warningRaised = false; }
            if (_latest == null || _latest.Lat != latitude || _latest.Lon != longitude ||
                !ReferenceEquals(_latest.View, view)) _faulted = false;
            _latest = next;
            _activeView = ReferenceEquals(MapView.Active, view);
            schedule = _activeView && !_suspended && !_faulted;
        }
        if (schedule) PanoramaOverlayPump.Request(this);
    }

    // Retained for compatibility with an older page that only sends positions.
    internal Task UpdateAsync(MapView view, double latitude, double longitude)
    {
        UpdateCamera(view, latitude, longitude, null, null);
        return Task.CompletedTask;
    }

    internal Task ClearAsync()
    {
        lock (_gate)
        {
            _latest = null;
            _mapRevision++;
            _warningRaised = false;
            _faulted = false;
        }
        PanoramaOverlayPump.Request(this);
        return Task.CompletedTask;
    }

    internal void SetSuspended(bool suspended)
    {
        bool schedule;
        lock (_gate)
        {
            if (_disposed || _suspended == suspended) return;
            _suspended = suspended;
            _mapRevision++;
            schedule = !suspended && _activeView && _latest != null;
        }
        if (schedule) PanoramaOverlayPump.Request(this);
    }

    private void OnCameraChanged(MapViewCameraChangedEventArgs args)
    {
        bool schedule;
        lock (_gate)
        {
            if (_disposed || !ReferenceEquals(_latest?.View, args.MapView)) return;
            _mapRevision++;
            _faulted = false;
            schedule = _activeView && !_suspended;
        }
        if (schedule) PanoramaOverlayPump.Request(this);
    }

    private void OnActiveViewChanged(ActiveMapViewChangedEventArgs args)
    {
        bool schedule;
        lock (_gate)
        {
            if (_disposed) return;
            _activeView = _latest != null && ReferenceEquals(_latest.View, args.IncomingView);
            _mapRevision++;
            _faulted = false;
            schedule = _activeView && !_suspended;
        }
        if (schedule) PanoramaOverlayPump.Request(this);
    }

    // Called only by the shared pump, within its single QueuedTask.
    internal void RenderLatestOnMct()
    {
        State? state;
        long revision;
        bool suspended;
        lock (_gate)
        {
            state = _disposed ? null : _latest;
            revision = _mapRevision;
            suspended = _suspended || !_activeView || _faulted;
        }
        if (state == null) { RemoveCore(); return; }
        if (suspended) return;
        try
        {
            var map = state.View.Map;
            if (map == null || map.MapType != MapType.Map)
            {
                RemoveCore();
                WarnOnce("Ukazatel panoramatu je dostupn\u00fd jen ve 2D map\u011b.");
                return;
            }
            var sr = map.SpatialReference;
            if (sr == null) throw new InvalidOperationException("Unknown spatial reference");
            if (!ReferenceEquals(_renderView, state.View))
            {
                RemoveCore();
                _renderView = state.View;
            }
            EnsureSymbols();

            bool positionChanged = _basis == null || _basis.Lat != state.Lat ||
                _basis.Lon != state.Lon || !_basis.SR.IsEqual(sr);
            if (positionChanged)
            {
                _basis = CreateBasis(state, sr);
                _screenRevision = -1;
            }
            var basis = _basis!;

            // Two geographic directions are projected only when position/CRS changes.
            // Camera rotation and S-JTSK meridian convergence are handled by
            // projecting these same basis vectors to the view, not by assuming +Y=north.
            if (_screenRevision != revision)
            {
                var c = state.View.MapToClient(basis.Center);
                var e = state.View.MapToClient(basis.East);
                var n = state.View.MapToClient(basis.North);
                _screenEX = (e.X - c.X) / 10; _screenEY = (e.Y - c.Y) / 10;
                _screenNX = (n.X - c.X) / 10; _screenNY = (n.Y - c.Y) / 10;
                _screenRevision = revision;
            }

            double radius = 0;
            bool drawSector = state.Heading.HasValue && state.Fov.HasValue;
            if (drawSector)
            {
                // Use the largest singular value of the local display transformation so
                // the whole sector stays bounded in non-conformal projections too.
                double ee = _screenEX * _screenEX + _screenEY * _screenEY;
                double nn = _screenNX * _screenNX + _screenNY * _screenNY;
                double en = _screenEX * _screenNX + _screenEY * _screenNY;
                var pixelsPerMetre = Math.Sqrt((ee + nn + Math.Sqrt(
                    (ee - nn) * (ee - nn) + 4 * en * en)) / 2);
                if (!double.IsFinite(pixelsPerMetre) || pixelsPerMetre <= 0)
                    throw new InvalidOperationException("Invalid map display scale");
                radius = Math.Min(Math.Clamp(48 / pixelsPerMetre, 5, 20), 64 / pixelsPerMetre);
                drawSector = double.IsFinite(radius) && radius > 0;
            }

            // Avoid rebuilding unchanged symbols/geometry, including point geometry
            // when only the panorama direction changes.
            bool coneChanged = positionChanged || _drawn?.Heading != state.Heading ||
                _drawn?.Fov != state.Fov || Math.Abs(_drawnRadius - radius) > 1e-9;
            bool createdCone = false;
            if (drawSector && (coneChanged || _sector == null || _axis == null))
            {
                createdCone = _sector == null || _axis == null;
                var angle = state.Heading!.Value;
                var fov = state.Fov!.Value;
                const int steps = 24; // only 27 vertices including center and closure
                var ring = new List<MapPoint>(steps + 3) { basis.Center };
                for (int i = 0; i <= steps; ++i)
                    ring.Add(Offset(basis, radius, angle - fov / 2 + fov * i / steps));
                ring.Add(basis.Center);
                var polygon = PolygonBuilderEx.CreatePolygon(ring, AttributeFlags.None, sr);
                var line = PolylineBuilderEx.CreatePolyline(
                    new[] { basis.Center, Offset(basis, radius, angle) }, AttributeFlags.None, sr);
                Upsert(state.View, ref _sector, polygon, _sectorSymbol!);
                Upsert(state.View, ref _axis, line, _axisSymbol!);
            }
            else if (!drawSector)
            {
                Release(ref _sector); Release(ref _axis);
            }

            // A newly added sector should stay beneath the position point.
            // Reinsert the point only when the sector is first created, not every frame.
            if (createdCone) Release(ref _dot);
            if (positionChanged || _dot == null)
                Upsert(state.View, ref _dot, basis.Center, _dotSymbol!);
            _drawn = state;
            _drawnRadius = radius;
        }
        catch (Exception ex)
        {
            RemoveCore();
            Debug.WriteLine("Panorama overlay: " + ex.GetType().Name);
            WarnOnce("Panorama je otev\u0159en\u00e9, ale ukazatel se nepoda\u0159ilo vykreslit. " +
                     "Otev\u0159ete 2D mapu a klikn\u011bte do n\u00ed znovu.");
        }
    }

    private static Basis CreateBasis(State state, SpatialReference sr)
    {
        var origin = MapPointBuilderEx.CreateMapPoint(state.Lon, state.Lat, SpatialReferences.WGS84);
        var origins = new[] { origin };
        var east = GeometryEngine.Instance.GeodeticMove(origins, SpatialReferences.WGS84,
            10, LinearUnit.Meters, Math.PI / 2, GeodeticCurveType.Geodesic);
        var north = GeometryEngine.Instance.GeodeticMove(origins, SpatialReferences.WGS84,
            10, LinearUnit.Meters, 0, GeodeticCurveType.Geodesic);
        if (east.Count != 1 || north.Count != 1) throw new InvalidOperationException("Projection horizon");
        MapPoint Project(MapPoint point) => GeometryEngine.Instance.Project(point, sr) as MapPoint
            ?? throw new InvalidOperationException("Point projection failed");
        var c = Project(origin); var e = Project(east[0]); var n = Project(north[0]);
        if (c.IsEmpty || e.IsEmpty || n.IsEmpty || !double.IsFinite(c.X) || !double.IsFinite(c.Y))
            throw new InvalidOperationException("Empty projected point");
        // A local tangent-plane approximation over at most 20 metres. This is a
        // direction indicator, NOT a visibility analysis or survey measurement.
        return new Basis(sr, c, e, n, (e.X - c.X) / 10, (e.Y - c.Y) / 10,
            (n.X - c.X) / 10, (n.Y - c.Y) / 10, state.Lat, state.Lon);
    }

    private static MapPoint Offset(Basis basis, double metres, double bearingDegrees)
    {
        var angle = bearingDegrees * Math.PI / 180;
        var e = metres * Math.Sin(angle); var n = metres * Math.Cos(angle);
        return MapPointBuilderEx.CreateMapPoint(basis.Center.X + e * basis.EX + n * basis.NX,
            basis.Center.Y + e * basis.EY + n * basis.NY, basis.SR);
    }

    private void EnsureSymbols()
    {
        if (_dotSymbol != null) return;
        double r = _provider == PanoramaProvider.Mapy ? 0 : 220;
        double g = _provider == PanoramaProvider.Mapy ? 120 : 125;
        double b = _provider == PanoramaProvider.Mapy ? 235 : 0;
        CIMColor Color(double alpha) => ColorFactory.Instance.CreateRGBColor(r, g, b, alpha);
        var outline = SymbolFactory.Instance.ConstructStroke(Color(65), 0.8, SimpleLineStyle.Solid);
        _sectorSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(Color(30),
            SimpleFillStyle.Solid, outline).MakeSymbolReference();
        _axisSymbol = SymbolFactory.Instance.ConstructLineSymbol(
            ColorFactory.Instance.CreateRGBColor(r * 0.72, g * 0.72, b * 0.72, 80),
            1, SimpleLineStyle.Solid).MakeSymbolReference();
        var dot = SymbolFactory.Instance.ConstructPointSymbol(Color(85), 10, SimpleMarkerStyle.Circle);
        // Real outline, not an opaque white disc underneath the transparent fill.
        foreach (var layer in dot.SymbolLayers)
            if (layer is CIMVectorMarker marker && marker.MarkerGraphics != null)
                foreach (var graphic in marker.MarkerGraphics)
                    graphic.Symbol = SymbolFactory.Instance.ConstructPolygonSymbol(Color(85),
                        SimpleFillStyle.Solid, SymbolFactory.Instance.ConstructStroke(
                            ColorFactory.Instance.CreateRGBColor(255, 255, 255, 95), 1, SimpleLineStyle.Solid));
        _dotSymbol = dot.MakeSymbolReference();
    }

    private static void Upsert(MapView view, ref IDisposable? handle, Geometry geometry, CIMSymbolReference symbol)
    {
        if (handle != null && view.UpdateOverlay(handle, geometry, symbol)) return;
        Release(ref handle);
        handle = view.AddOverlay(geometry, symbol);
    }

    private static void Release(ref IDisposable? handle)
    {
        try { handle?.Dispose(); }
        catch { /* View may have been closed. */ }
        finally { handle = null; }
    }

    private void RemoveCore()
    {
        Release(ref _dot); Release(ref _axis); Release(ref _sector);
        _basis = null; _drawn = null; _renderView = null; _screenRevision = -1;
    }

    private void WarnOnce(string message)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _faulted = true;
            if (_warningRaised) return;
            _warningRaised = true;
        }
        try { Warning?.Invoke(message); } catch { }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true; _latest = null;
        }
        MapViewCameraChangedEvent.Unsubscribe(_cameraSubscription);
        ActiveMapViewChangedEvent.Unsubscribe(_activeSubscription);
        Warning = null;
        PanoramaOverlayPump.Request(this);
    }
}

/// <summary>
/// Shared by BOTH providers. At most one scheduled/running QueuedTask for these
/// overlays. Pending work is a set of windows, never a queue of camera positions.
/// No timer/task remains active after the pending set is drained.
/// </summary>
internal static class PanoramaOverlayPump
{
    private static readonly object Gate = new();
    private static readonly HashSet<PanoramaPositionOverlay> Pending = new();
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static bool _running;
    private static double _lastStart = -100;
    private static double _lastWorkMs;

    internal static void Request(PanoramaPositionOverlay overlay)
    {
        bool start;
        lock (Gate)
        {
            Pending.Add(overlay);
            start = !_running;
            if (start) _running = true;
        }
        if (start) _ = DrainAsync();
    }

    private static async Task DrainAsync()
    {
        while (true)
        {
            // If rendering was expensive, back off instead of saturating the MCT.
            // Excludes the time merely waiting in the ArcGIS QueuedTask queue.
            var interval = Math.Clamp(Math.Max(100, _lastWorkMs * 4), 100, 5000);
            var delay = interval - (Clock.Elapsed.TotalMilliseconds - _lastStart);
            if (delay > 0) await Task.Delay((int)Math.Ceiling(delay)).ConfigureAwait(false);
            PanoramaPositionOverlay[] batch;
            lock (Gate)
            {
                if (Pending.Count == 0) { _running = false; return; }
                batch = Pending.ToArray();
                Pending.Clear();
            }
            try
            {
                await QueuedTask.Run(() =>
                {
                    _lastStart = Clock.Elapsed.TotalMilliseconds;
                    var work = Stopwatch.StartNew();
                    foreach (var overlay in batch) overlay.RenderLatestOnMct();
                    _lastWorkMs = work.Elapsed.TotalMilliseconds;
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Never leave _running latched after shutdown or a scheduler error.
                Debug.WriteLine("Panorama overlay scheduler: " + ex.GetType().Name);
            }
            lock (Gate)
            {
                if (Pending.Count == 0) { _running = false; return; }
            }
        }
    }
}
