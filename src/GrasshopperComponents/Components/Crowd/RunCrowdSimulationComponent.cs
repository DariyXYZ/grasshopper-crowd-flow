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
        : base("Run Crowd Simulation", "Solve", "Runs the crowd solver and returns trajectories, timeline data, and core reporting metrics.", "Crowd Flow", "Crowd")
    {
    }

    public override Guid ComponentGuid => new("93d67d3f-16cb-4730-8712-18c8027e2f6d");

    protected override bool IsDeveloperOnly => false;

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdRun;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Crowd Model", "M", "Crowd model assembled by Create Crowd Model.", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Agent Positions", "P", "Active agent positions per frame.", GH_ParamAccess.tree);
        pManager.AddCurveParameter("Trajectories", "T", "Agent trajectories across the simulation.", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Active Counts", "A", "Active agent count per frame.", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Finished Counts", "F", "Finished agent count per frame.", GH_ParamAccess.list);
        pManager.AddNumberParameter("Clearance Time", "CT", "Scenario clearance time in seconds.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Mean Travel Time", "MT", "Mean travel time in seconds for completed agents.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Minimum Travel Time", "MinT", "Minimum travel time in seconds for completed agents.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Maximum Travel Time", "MaxT", "Maximum travel time in seconds for completed agents.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Exit Split", "ES", "Completed-agent share by exit index as values from 0 to 1.", GH_ParamAccess.list);
        pManager.AddBooleanParameter("Completed All", "C", "True when every spawned agent reached an exit.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Core Metrics", "CM", "Core simulation metrics summary for downstream reporting.", GH_ParamAccess.item);
        pManager.AddGenericParameter("Result", "R", "Full crowd simulation result object.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Exit Indices", "EI", "Exit indices corresponding to the Exit Split values.", GH_ParamAccess.list);
        pManager.AddIntegerParameter("Exit Counts", "EC", "Completed-agent counts corresponding to the Exit Split values.", GH_ParamAccess.list);
        pManager.AddTextParameter("Profile", "Prof", "Simulation stage timings for debugging and optimization.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<object> modelInputs = new();

        if (!DA.GetDataList(0, modelInputs) || modelInputs.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A crowd model is required.");
            return;
        }

        if (!GhObjectExtraction.TryExtract(modelInputs[0], out CrowdModel? model) || model == null)
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
            CrowdSimulationCoreMetrics core = result.CoreMetrics;
            List<int> exitIndices = core.ExitSplits.Select(split => split.ExitIndex).ToList();
            List<int> exitCounts = core.ExitSplits.Select(split => split.CompletedAgents).ToList();
            List<double> exitSplit = core.ExitSplits.Select(split => split.Share).ToList();

            DA.SetDataTree(0, positionsTree);
            DA.SetDataList(1, trajectories);
            DA.SetDataList(2, activeCounts);
            DA.SetDataList(3, finishedCounts);
            DA.SetData(4, core.ClearanceTime);
            DA.SetData(5, core.MeanTravelTime);
            DA.SetData(6, core.MinimumTravelTime);
            DA.SetData(7, core.MaximumTravelTime);
            DA.SetDataList(8, exitSplit);
            DA.SetData(9, result.CompletedAllAgents);
            DA.SetData(10, core);
            DA.SetData(11, result);
            DA.SetDataList(12, exitIndices);
            DA.SetDataList(13, exitCounts);
            DA.SetData(14, result.Profile.ToString());
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}
