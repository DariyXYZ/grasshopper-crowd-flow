using Crowd.Models;
using Crowd.Services;
using Grasshopper.Kernel;
using GrasshopperComponents.Utilities;
using Rhino.Geometry;
using System.Drawing;

namespace GrasshopperComponents.Components.Crowd;

public sealed class PreviewCrowdAgentsComponent : IndGhComponent
{
    private DateTime _startedUtc;
    private CrowdSimulationResult? _activeResult;
    private bool _wasPlaying;

    public PreviewCrowdAgentsComponent()
        : base("Preview Crowd Agents", "Agents", "Animates static pedestrian meshes from a crowd simulation result.", "Crowd Flow", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("2bd74c60-07ab-4b73-a6f4-56b04342f02d");

    protected override bool IsDeveloperOnly => false;

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdPeoplePreview;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Result", "R", "Full crowd simulation result from Run Crowd Simulation.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Preview", "P", "True to play the pedestrian preview; false to hide it.", GH_ParamAccess.item, true);
        pManager.AddNumberParameter("Speed", "S", "Playback speed multiplier.", GH_ParamAccess.item, 5.0);
        pManager.AddBooleanParameter("Loop", "L", "Restart playback after the simulated duration.", GH_ParamAccess.item, true);
        pManager.AddColourParameter("Color", "C", "Pedestrian mesh color.", GH_ParamAccess.item, Color.White);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddMeshParameter("People", "G", "Static pedestrian meshes for the current playback frame.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Time", "T", "Current simulation playback time in seconds.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Active Count", "A", "Number of visible active agents in the current playback frame.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        object? resultInput = null;
        bool preview = true;
        double speed = 5.0;
        bool loop = true;
        Color color = Color.White;

        if (!DA.GetData(0, ref resultInput) || !GhObjectExtraction.TryExtract(resultInput, out CrowdSimulationResult? result) || result == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A CrowdSimulationResult from Run Crowd Simulation is required.");
            return;
        }

        DA.GetData(1, ref preview);
        DA.GetData(2, ref speed);
        DA.GetData(3, ref loop);
        DA.GetData(4, ref color);

        if (!preview)
        {
            _wasPlaying = false;
            DA.SetDataList(0, Array.Empty<Mesh>());
            DA.SetData(1, 0.0);
            DA.SetData(2, 0);
            return;
        }

        if (!_wasPlaying || !ReferenceEquals(_activeResult, result))
        {
            _startedUtc = DateTime.UtcNow;
            _activeResult = result;
            _wasPlaying = true;
        }

        speed = Math.Max(0.01, Math.Min(speed, 100.0));
        double duration = Math.Max(0.0, result.SimulatedDuration);
        double elapsed = (DateTime.UtcNow - _startedUtc).TotalSeconds * speed;
        double simulationTime = ResolveSimulationTime(elapsed, duration, loop);

        CrowdAgentPreviewFrame frame = CrowdAgentPreviewService.CreateFrame(result, simulationTime, color);

        DA.SetDataList(0, frame.AgentMeshes);
        DA.SetData(1, frame.SimulationTime);
        DA.SetData(2, frame.ActiveCount);

        if (loop || elapsed <= duration)
        {
            OnPingDocument()?.ScheduleSolution(33, _ => ExpireSolution(false));
        }
    }

    private static double ResolveSimulationTime(double elapsed, double duration, bool loop)
    {
        if (duration <= 0.0)
        {
            return 0.0;
        }

        if (loop)
        {
            return elapsed % duration;
        }

        return Math.Max(0.0, Math.Min(elapsed, duration));
    }
}
