namespace Crowd.Models;

public sealed class CrowdModel
{
    public CrowdModel(
        CrowdFloor floor,
        IReadOnlyList<CrowdObstacle> obstacles,
        IReadOnlyList<CrowdSource> sources,
        IReadOnlyList<CrowdExit> exits,
        CrowdAgentProfile agentProfile,
        double timeStep)
    {
        Floor = floor ?? throw new ArgumentNullException(nameof(floor));
        Obstacles = obstacles ?? throw new ArgumentNullException(nameof(obstacles));
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        Exits = exits ?? throw new ArgumentNullException(nameof(exits));
        AgentProfile = agentProfile ?? throw new ArgumentNullException(nameof(agentProfile));
        TimeStep = timeStep;
    }

    public CrowdFloor Floor { get; }

    public IReadOnlyList<CrowdObstacle> Obstacles { get; }

    public IReadOnlyList<CrowdSource> Sources { get; }

    public IReadOnlyList<CrowdExit> Exits { get; }

    public CrowdAgentProfile AgentProfile { get; }

    public double TimeStep { get; }
}
