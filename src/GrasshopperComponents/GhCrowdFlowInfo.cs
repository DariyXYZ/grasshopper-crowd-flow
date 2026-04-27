using Grasshopper.Kernel;
using System.Drawing;

namespace GrasshopperComponents;

public sealed class GhCrowdFlowInfo : GH_AssemblyInfo
{
    public override string Name => "Crowd Flow";

    public override string Description => "Grasshopper plugin for crowd movement simulation and architectural heatmap analysis in Rhino 8.";

    public override Guid Id => new("9fd6d4d0-7f2d-4a25-b6cb-3b124c2b79fb");

    public override string AssemblyVersion => GetType().Assembly.GetName().Version?.ToString() ?? "0.1.0";

    public override string AuthorName => "DariyXYZ";

    public override string AuthorContact => "https://github.com/DariyXYZ";

    public override Bitmap Icon => Properties.Resources.PluginIcon;
}
