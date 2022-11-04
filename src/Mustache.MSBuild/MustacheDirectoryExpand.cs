namespace Mustache.MSBuild;

using Microsoft.Build.Framework;
using Mustache;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Task that expand templates using Mustache.
/// </summary>
public class MustacheDirectoryExpand : Microsoft.Build.Utilities.Task
{
    private Template? template;
    private IDictionary<string, object>? rootData = new Dictionary<string, object>();

    public string TemplateFile { get; set; } = "template.mustache";

    public string DestinationRootDirectory { get; set; } = string.Empty;

    public string DefaultDestinationFileName { get; set; } = "expanded.txt";

    public string DataRootDirectory { get; set; } = "data";

    public string DirectoryStructureFile { get; set; } = "data-directory-structure.json";

    public string DefaultDataFileName { get; set; } = "data.json";

    public bool LeafExpansion { get; set; } = true;

    public string InputData { get; set; } = string.Empty;

    public override bool Execute()
    {
        this.NormalizePaths();

        // Parse and compile the template.
        var templateString = File.ReadAllText(this.TemplateFile);
        this.template = Template.Compile(templateString);

        // Parse and deserialize the directory structure.
        using var directoryTreeStream = File.OpenRead(this.DirectoryStructureFile);
        var directoryTree = JsonSerializer.Deserialize<DirectoryTree>(directoryTreeStream);

        // Load and parse the root data file.
        var rootDataFile = Path.Combine(this.DataRootDirectory, this.DefaultDataFileName);

        if (!File.Exists(rootDataFile))
        {
            this.Log.LogError($"Could not find root data file, '{rootDataFile}'. This is required to do mustache replacements.");
            return false;
        }

        using var dataStream = File.OpenRead(rootDataFile);
        this.rootData = JsonSerializer.Deserialize<Dictionary<string, object>>(dataStream);

        // Parse the input data and load it in the root data dictionary.
        this.ParseInputData();

        // Traverse the data structure and expand the template on every level as defined.
        // The content of the data file is added along with the data from the directories above and passed to the template.
        // The directory names will be accessible using {{DirectoryNameOfLevelN}}, where N is the depth in the directory tree.
        var nodes = directoryTree?.Children.SelectMany(item => Traverse(item, directory => directory.Children, node => this.ExpandTemplate(node)));
        var count = nodes?.Count() ?? 0;
        return true;
    }

    private static IEnumerable<Node<T>> Traverse<T>(T item, Func<T, IEnumerable<T>> childSelector, Func<Node<T>, bool> visitor)
        where T : IItem<T>
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
        where T : IItem<T>
    {
        var parentData = node.Parent?.ResolvedData;
        if (parentData == null)
        {
            parentData = this.rootData;
        }

        var mergedData = parentData.Select(kv => kv);
        var dataFile = Path.Combine(this.DataRootDirectory, node.GetIdentifierChain(), this.DefaultDataFileName);
        if (File.Exists(dataFile))
        {
            using var dataStream = File.OpenRead(dataFile);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataStream);
            if (data != null)
            {
                mergedData = data.Concat(parentData.Where(x => !data.ContainsKey(x.Key)));
            }
        }
        else
        {
            this.Log.LogMessage(MessageImportance.Normal, $"Data file {dataFile} was not located. Will use parent data.");
        }

        var outputDirectory = Path.Combine(this.DestinationRootDirectory, node.GetIdentifierChain());
        this.Log.LogMessage(MessageImportance.Normal, $"Resulting directory is '{outputDirectory}'");

        node.ResolvedData = mergedData.ToDictionary(kv => kv.Key, kv => kv.Value);
        node.ResolvedData.Add($"DirectoryNameOfLevel{node.Level}", node.Item.Id);

        var renderedTemplate = this.template?.Render(node.ResolvedData);

        System.IO.Directory.CreateDirectory(outputDirectory);

        if (this.LeafExpansion && node.Item.Children.Any())
        {
            return true;
        }

        File.WriteAllText(Path.Combine(outputDirectory, this.DefaultDestinationFileName), renderedTemplate?.TrimEnd());
        return true;
    }

    private void ParseInputData()
    {
        if (string.IsNullOrEmpty(this.InputData))
        {
            return;
        }

        var data = this.InputData.Split(';');
        foreach (var item in data)
        {
            var keyValue = item.Split('=');
            if (keyValue.Length != 2)
            {
                this.Log.LogError($"Invalid input data, '{item}'.");
                continue;
            }

            this.rootData.Add(keyValue[0], keyValue[1]);
        }
    }

    private void NormalizePaths()
    {
        this.DestinationRootDirectory = this.DestinationRootDirectory
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Trim();

        this.DataRootDirectory = this.DataRootDirectory
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Trim();
    }
}

internal interface IItem<T>
{
    public string Id { get; }

    public IReadOnlyCollection<T> Children { get; }
}

internal class Node<T>
    where T : IItem<T>
{
    public Node(T item, int level, Node<T>? parent) => (this.Item, this.Level, this.Parent) = (item, level, parent);

    public T Item { get; }

    public Node<T>? Parent { get; }

    public int Level { get; }

    public IDictionary<string, object> ResolvedData { get; set; } = new Dictionary<string, object>();

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

internal class DirectoryTree : IItem<Directory>
{
    public DirectoryTree(IReadOnlyCollection<Directory> children, string dataFileName)
    {
        this.Children = children ?? Array.Empty<Directory>();
        this.DataFileName = dataFileName;
    }

    public IReadOnlyCollection<Directory> Children { get; }

    public string DataFileName { get; }

    public string Id => throw new NotImplementedException();
}

internal class Directory : IItem<Directory>
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
}
