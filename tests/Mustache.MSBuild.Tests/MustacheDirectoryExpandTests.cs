namespace Mustache.MSBuild;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;

public class MustacheDirectoryExpandTests
{
    private readonly Mock<IBuildEngine> buildEngineMock = new ();
    private readonly List<BuildErrorEventArgs> errors = new ();

    public MustacheDirectoryExpandTests()
    {
        this.buildEngineMock.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
            .Callback<BuildErrorEventArgs>(x => this.errors.Add(x));
    }

    [Fact]
    public void Execute_DefaultParametersDirectoryExpansion_Success()
    {
        var task = new MustacheDirectoryExpand
        {
            BuildEngine = this.buildEngineMock.Object,
            TemplateFile = "mustache-templates/directory.mustache",
            LeafExpansion = false,
        };

        var result = task.Execute();

        Assert.True(result);

        var directoryStructure = JsonSerializer.Deserialize<Directory>(
            File.ReadAllText("directory-structure.json"),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        var level1Paths = directoryStructure?.Children.Select(d => File.Exists(Path.Combine(d.Name, "expanded.txt")));
        Assert.DoesNotContain(false, level1Paths);
        var level2Paths = directoryStructure?.Children.SelectMany(d => d.Children.Select(e => File.Exists(Path.Combine(d.Name, e.Name, "expanded.txt"))));
        Assert.DoesNotContain(false, level2Paths);
    }

    [Fact]
    public void Execute_LeafDirectoryExpansion_Success()
    {
        var task = new MustacheDirectoryExpand
        {
            BuildEngine = this.buildEngineMock.Object,
            TemplateFile = "mustache-templates/directory.mustache",
            DestinationRootDirectory = "leaf-expansion",
        };

        var result = task.Execute();

        Assert.True(result);

        var directoryStructure = JsonSerializer.Deserialize<Directory>(
            File.ReadAllText("directory-structure.json"),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        var level1Paths = directoryStructure?.Children
            .Select(d => File.Exists(Path.Combine("leaf-expansion", d.Name, "expanded.txt")))
            .Where(p => p is true);
        Assert.Equal(1, level1Paths?.Count());

        var level2Paths = directoryStructure?.Children
            .SelectMany(d => d.Children.Select(e => File.Exists(Path.Combine("leaf-expansion", d.Name, e.Name, "expanded.txt"))));
        Assert.DoesNotContain(false, level2Paths);
    }

    public void Dispose()
    {
        this.buildEngineMock.VerifyAll();
    }
}

internal class DirectoryTree
{
    public DirectoryTree(IReadOnlyCollection<Directory> children)
    {
        this.Children = children ?? Array.Empty<Directory>();
    }

    public IReadOnlyCollection<Directory> Children { get; }
}

internal class Directory
{
    [JsonConstructor]
    public Directory(string name, IReadOnlyCollection<Directory> children)
    {
        this.Name = name;
        this.Children = children ?? Array.Empty<Directory>();
    }

    public string Name { get; }

    public IReadOnlyCollection<Directory> Children { get; }
}