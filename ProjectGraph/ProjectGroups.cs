namespace Dsd.ProjectGraph;

public class ProjectGroups
{
    public required List<ProjectTree> All { get; init; }
    public required List<ProjectTree> Top { get; init; }
    public required List<ProjectTree> Level1 { get; init; }
    public required List<ProjectTree> Level2 { get; init; }
    public required List<ProjectTree> Bottom { get; init; }
}
