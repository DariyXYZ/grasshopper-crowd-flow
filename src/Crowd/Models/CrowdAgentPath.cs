using Rhino.Geometry;

namespace Crowd.Models;

public sealed class CrowdAgentPath
{
    public CrowdAgentPath(int agentId, int exitIndex, Polyline polyline, bool reachedExit, double spawnTime, double? finishTime)
    {
        AgentId = agentId;
        ExitIndex = exitIndex;
        Polyline = polyline ?? throw new ArgumentNullException(nameof(polyline));
        ReachedExit = reachedExit;
        SpawnTime = spawnTime;
        FinishTime = finishTime;
    }

    public int AgentId { get; }

    public int ExitIndex { get; }

    public Polyline Polyline { get; }

    public bool ReachedExit { get; }

    public double SpawnTime { get; }

    public double? FinishTime { get; }
}
