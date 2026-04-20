namespace GrasshopperComponents.Properties;

[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Manual", "1.0.0.0")]
[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
internal class Resources
{
    private static global::System.Resources.ResourceManager? resourceMan;
    private static global::System.Globalization.CultureInfo? resourceCulture;

    internal Resources()
    {
    }

    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static global::System.Resources.ResourceManager ResourceManager
    {
        get
        {
            resourceMan ??= new global::System.Resources.ResourceManager("GrasshopperComponents.Properties.Resources", typeof(Resources).Assembly);
            return resourceMan;
        }
    }

    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static global::System.Globalization.CultureInfo? Culture
    {
        get => resourceCulture;
        set => resourceCulture = value;
    }

    private static System.Drawing.Bitmap GetBitmap(string name)
    {
        object obj = ResourceManager.GetObject(name, resourceCulture)!;
        return (System.Drawing.Bitmap)obj;
    }

    internal static System.Drawing.Bitmap PluginIcon => GetBitmap("PluginIcon");
    internal static System.Drawing.Bitmap CrowdAgent => GetBitmap("CrowdAgent");
    internal static System.Drawing.Bitmap CrowdExit => GetBitmap("CrowdExit");
    internal static System.Drawing.Bitmap CrowdFloor => GetBitmap("CrowdFloor");
    internal static System.Drawing.Bitmap CrowdHeatmap => GetBitmap("CrowdHeatmap");
    internal static System.Drawing.Bitmap CrowdHeatmapLegend => GetBitmap("CrowdHeatmapLegend");
    internal static System.Drawing.Bitmap CrowdModel => GetBitmap("CrowdModel");
    internal static System.Drawing.Bitmap CrowdObstacle => GetBitmap("CrowdObstacle");
    internal static System.Drawing.Bitmap CrowdRun => GetBitmap("CrowdRun");
    internal static System.Drawing.Bitmap CrowdSource => GetBitmap("CrowdSource");
    internal static System.Drawing.Bitmap CrowdExportImage => GetBitmap("CrowdExportImage");
    internal static System.Drawing.Bitmap CrowdExportReport => GetBitmap("CrowdExportReport");
}
