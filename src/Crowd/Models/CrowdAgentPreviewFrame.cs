using Rhino.Geometry;

namespace Crowd.Models;

/// <summary>Geometry snapshot used for animated agent preview playback.</summary>
public sealed class CrowdAgentPreviewFrame
{
    public CrowdAgentPreviewFrame(
        IReadOnlyList<Mesh> agentMeshes,
        IReadOnlyList<Point3d> agentPositions,
        double simulationTime,
        int activeCount)
    {
        AgentMeshes = agentMeshes ?? throw new ArgumentNullException(nameof(agentMeshes));
        AgentPositions = agentPositions ?? throw new ArgumentNullException(nameof(agentPositions));
        SimulationTime = simulationTime;
        ActiveCount = activeCount;
    }

    /// <summary>Static display meshes for the active agents at this playback time.</summary>
    public IReadOnlyList<Mesh> AgentMeshes { get; }

    /// <summary>Active agent positions at this playback time.</summary>
    public IReadOnlyList<Point3d> AgentPositions { get; }

    /// <summary>Playback time in simulation seconds.</summary>
    public double SimulationTime { get; }

    /// <summary>Number of active agents represented in this frame.</summary>
    public int ActiveCount { get; }
}
