using Microsoft.Build.Construction;

namespace Dsd.ProjectGraph;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: ProjectGraph <SolutionFile> <OutputFile>");
            return 1;
        }

        var projectTree = ParseSolution(args[0]);
        var projectGroups = GroupProjects(projectTree);

        if (File.Exists(args[1]))
        {
            Console.WriteLine("Output file alredy exists: " + args[1]);
            return 2;
        }

        File.WriteAllText(args[1], CreateMermaid(projectGroups));
        return 0;
    }

    private static Dictionary<string, ProjectTree> ParseSolution(string solutionFile)
    {
        var solution = SolutionFile.Parse(solutionFile);

        var dictionary = new Dictionary<string, ProjectTree>();

        foreach (var project in solution.ProjectsInOrder)
        {
            // Console.WriteLine($"{project.ProjectName} is of type: {project.ProjectType} {project.AbsolutePath} ");

            if (project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
            {
                dictionary.Add(project.AbsolutePath, new ProjectTree
                {
                    ProjectName = project.ProjectName,
                    NodeName = project.ProjectName
                });
            }
        }

        foreach (var (absolutePath, projectTree) in dictionary)
        {
            ProjectRootElement projectRoot = ProjectRootElement.Open(absolutePath);

            var projectReferences = projectRoot.Items.Where(x => x.ItemType.Equals("ProjectReference"));
            foreach (var projectReference in projectReferences)
            {
                var fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(absolutePath)!, projectReference.Include));

                if (dictionary.TryGetValue(fullPath, out var prjref))
                {
                    projectTree.References.Add(prjref);
                    prjref.ReferencedBy.Add(projectTree);
                }
                else
                {
                    Console.WriteLine($"-- Unable to find project reference {fullPath}");
                }
            }
        }

        return dictionary;
    }

    private static ProjectGroups GroupProjects(Dictionary<string, ProjectTree> projectTree)
    {
        var all = projectTree.Values.ToList();
        var top = all.Where(x => x.ReferencedBy.Count == 0).ToList();
        var bottom = all.Where(x => x.References.Count == 0).ToList();
        var middle = all.Except(top).Except(bottom).ToList();
        var level2 = middle.Where(x => x.ReferencedBy.Except(top).Any()).ToList();
        var level1 = middle.Except(level2).ToList();

        return new ProjectGroups()
        {
            All = all,
            Top = top,
            Level1 = level1,
            Level2 = level2,
            Bottom = bottom,
        };
    }

    private static string CreateMermaid(ProjectGroups projectGroups)
    {
        var writer = new StringWriter();

        writer.WriteLine("```mermaid");
        writer.WriteLine("%%{init: {\"flowchart\": {\"defaultRenderer\": \"elk\"}} }%%");
        writer.WriteLine("flowchart TD");

        WriteSubGraph(writer, "Top", projectGroups.Top);
        WriteSubGraph(writer, "Level1", projectGroups.Level1);
        WriteSubGraph(writer, "Level2", projectGroups.Level2);
        WriteSubGraph(writer, "Bottom", projectGroups.Bottom);

        WriteReferences(writer, projectGroups.All);

        writer.WriteLine("```");
        return writer.ToString();
    }

    private static void WriteSubGraph(StringWriter writer, string name, List<ProjectTree> list)
    {
        if (list.Count > 0)
        {
            writer.WriteLine("  subgraph " + name);
            foreach (var item in list)
            {
                writer.Write($"    {item.NodeName}");
                if (item.ProjectName != item.NodeName)
                {
                    writer.Write($"[\"{item.ProjectName}\"]");
                }
                writer.WriteLine(';');
            }
            writer.WriteLine("  end");
            writer.WriteLine();
        }
    }

    private static void WriteReferences(StringWriter writer, List<ProjectTree> list)
    {
        foreach (var item in list)
        {
            foreach (var reference in item.References)
            {
                writer.WriteLine($"  {item.NodeName}-->{reference.NodeName};");
            }
        }
    }
}