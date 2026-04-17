using Crowd.Models;
using Crowd.Services;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using GrasshopperComponents.Utilities;
using Rhino.Geometry;

namespace GrasshopperComponents.Components.Crowd;

public sealed class RunCrowdSimulationComponent : IndGhComponent
{
    public RunCrowdSimulationComponent()
        : base("Run Crowd Simulation", "Solve", "Runs the crowd solver until all agents reach exits and returns trajectories, frame data, and completion stats.", "GhCrowdFlow", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("93d67d3f-16cb-4730-8712-18c8027e2f6d");

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdRun;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Crowd Model", "M", "Crowd model assembled by Create Crowd Model.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Agent Positions", "P", "Active agent positions per frame.", GH_ParamAccess.tree);
        pManager.AddCurveParameter("Trajectories", "T", "Agent trajectories across the simulation.", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Active Counts", "A", "Active agent count per frame.", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Finished Counts", "F", "Finished agent count per frame.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Simulated Duration", "D", "Actual simulated time until completion or safety stop.", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Completed All", "C", "True when every spawned agent reached an exit.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Result", "R", "Crowd simulation result object.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        object? modelInput = null;

        if (!DA.GetData(0, ref modelInput) || modelInput == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A crowd model is required.");
            return;
        }

        if (!GhObjectExtraction.TryExtract(modelInput, out CrowdModel? model) || model == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to extract CrowdModel from input.");
            return;
        }

        try
        {
            CrowdSimulationResult result = CrowdSimulationService.Run(model);
            DataTree<Point3d> positionsTree = new();
            List<int> activeCounts = new();
            List<int> finishedCounts = new();

            for (int i = 0; i < result.Frames.Count; i++)
            {
                GH_Path path = new(i);
                positionsTree.AddRange(result.Frames[i].ActivePositions, path);
                activeCounts.Add(result.Frames[i].ActiveCount);
                finishedCounts.Add(result.Frames[i].FinishedCount);
            }

            List<PolylineCurve> trajectories = result.AgentPaths
                .Where(path => path.Polyline.Count >= 2)
                .Select(path => new PolylineCurve(path.Polyline))
                .ToList();

            DA.SetDataTree(0, positionsTree);
            DA.SetDataList(1, trajectories);
            DA.SetDataList(2, activeCounts);
            DA.SetDataList(3, finishedCounts);
            DA.SetData(4, result.SimulatedDuration);
            DA.SetData(5, result.CompletedAllAgents);
            DA.SetData(6, result);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}
