namespace Dsd.ProjectGraph;

public class ProjectTree
{
    public required string ProjectName { get; init; }
    public required string NodeName { get; init; }

    public List<ProjectTree> References { get; init; } = new List<ProjectTree>();
    public List<ProjectTree> ReferencedBy { get; init; } = new List<ProjectTree>();
}
