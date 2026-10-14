using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
namespace PanoramaViewer;
internal class Module1 : Module
{
    private static Module1? _this;
    public static Module1 Current => _this ??= (Module1)FrameworkApplication.FindModule("PanoramaViewer_Module");
    protected override bool CanUnload() => true;
}
