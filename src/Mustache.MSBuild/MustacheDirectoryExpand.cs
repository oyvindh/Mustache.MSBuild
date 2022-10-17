namespace Mustache.MSBuild;

using Mustache;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Task that expand templates using Mustache.
/// </summary>
public class MustacheDirectoryExpand : Microsoft.Build.Utilities.Task
{
    public string TeamplateFile { get; set; } = "template.mustache";

    public string DestinationRootDirectory { get; set; } = "output";

    public string DataRootDirectory { get; set; } = "data";

    public string DirectoryStructureFile { get; set; } = "directory-structure.json";

    public string DefaultDataFileName { get; set; } = "data.json";

    public override bool Execute()
    {
        // Parse and compile the template.
        var templateString = File.ReadAllText(this.TeamplateFile);
        var template = Template.Compile(templateString);

        // Parse and deserialize the directory structure.
        using var dataStream = File.OpenRead(this.DirectoryStructureFile);
        var directoryTree = JsonSerializer.Deserialize<DirectoryTree>(dataStream);

        // Traverse the data structure and expand the template on every level as defined.
        // The content of the data file is added along with the data from the directories above and passed to the template.
        // The directory names will be accessible using {{DirectoryNameOfLevelN}}, where N is the depth in the directory tree.
        var nodes = directoryTree?.Children.SelectMany(item => Traverse(item, directory => directory.Children, node => this.ExpandTemplate(node)));
        Console.WriteLine(nodes.Count());
        return true;
    }

    private static IEnumerable<Node<T>> Traverse<T>(T item, Func<T, IEnumerable<T>> childSelector, Func<Node<T>, bool> visitor)
        where T : IItem
    {
        var stack = new Stack<Node<T>>();
        stack.Push(new Node<T>(item, 1, null));
        while (stack.Any())
        {
            var next = stack.Pop();
            visitor(next);
            yield return next;
            foreach (var child in childSelector(next.Item))
            {
                stack.Push(new Node<T>(child, next.Level + 1, next));
            }
        }
    }

    private bool ExpandTemplate<T>(Node<T> node)
        where T : IItem
    {
        Console.WriteLine($"Resulting directory is {Path.Combine(this.DataRootDirectory, node.GetIdentifierChain())}");
        return true;
    }
}

internal interface IItem
{
    public string Id { get; }

    public IDictionary<string, object> State { get; }
}

internal class Node<T>
    where T : IItem
{
    public Node(T item, int level, Node<T>? parent) => (this.Item, this.Level, this.Parent) = (item, level, parent);

    public T Item { get; }

    public Node<T>? Parent { get; }

    public int Level { get; }

    public string GetIdentifierChain()
    {
        if (this.Parent != null)
        {
            return $"{this.Parent.GetIdentifierChain()}/{this.Item.Id}";
        }
        else
        {
            return this.Item.Id;
        }
    }
}

internal class DirectoryTree : IItem
{
    public DirectoryTree(IReadOnlyCollection<Directory> children, string dataFileName)
    {
        this.Children = children ?? Array.Empty<Directory>();
        this.DataFileName = dataFileName;
    }

    public IReadOnlyCollection<Directory> Children { get; }

    public string DataFileName { get; }

    public string Id => throw new NotImplementedException();

    public IDictionary<string, object> State => throw new NotImplementedException();
}

internal class Directory : IItem
{
    [JsonConstructor]
    public Directory(string name, IReadOnlyCollection<Directory> children, string dataFileName)
    {
        this.Name = name;
        this.Children = children ?? Array.Empty<Directory>();
        this.DataFileName = dataFileName;
    }

    public string Name { get; }

    public IReadOnlyCollection<Directory> Children { get; }

    public string DataFileName { get; }

    public string Id => this.Name;

    public IDictionary<string, object> State => throw new NotImplementedException();
}