namespace Mustache.MSBuild;

using System.Text.Json;
using Microsoft.Build.Framework;

public class MustacheExpandTests
{
    private readonly Mock<IBuildEngine> buildEngineMock = new ();
    private readonly List<BuildErrorEventArgs> errors = new ();

    public MustacheExpandTests()
    {
        this.buildEngineMock.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
            .Callback<BuildErrorEventArgs>(x => this.errors.Add(x));
    }

    public void Dispose()
    {
        this.buildEngineMock.VerifyAll();
    }

    [Fact]
    public void Execute_SimpleTemplate_Success()
    {
        var task = new MustacheExpand
        {
            BuildEngine = this.buildEngineMock.Object,
            TemplateFile = "mustache-templates/simple.mustache",
            DataFile = "data/simple.json",
            DestinationFile = "result.json",
        };

        var result = task.Execute();

        var item = JsonSerializer.Deserialize<SimpleItem>(
            File.ReadAllText(task.DestinationFile),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        Assert.True(result);
        Assert.NotNull(item);
        Assert.False(string.IsNullOrEmpty(item?.Name), "Expected name to be non-empty");
        Assert.Equal("Some name", item?.Name);
    }

    [Fact]
    public void Execute_SimpleTemplateWithInputOverride_Success()
    {
        var task = new MustacheExpand
        {
            BuildEngine = this.buildEngineMock.Object,
            TemplateFile = "mustache-templates/simple.mustache",
            DataFile = "data/simple.json",
            InputData = "name=Override",
            DestinationFile = "result.json",
        };

        var result = task.Execute();

        var item = JsonSerializer.Deserialize<SimpleItem>(
            File.ReadAllText(task.DestinationFile),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        Assert.True(result);
        Assert.NotNull(item);
        Assert.False(string.IsNullOrEmpty(item?.Name), "Expected name to be non-empty");
        Assert.Equal("Override", item?.Name);
    }

    [Fact]
    public void Execute_Array_Success()
    {
        var task = new MustacheExpand
        {
            BuildEngine = this.buildEngineMock.Object,
            TemplateFile = "mustache-templates/array.mustache",
            DataFile = "data/array.json",
            DestinationFile = "result.html",
        };

        var result = task.Execute();

        var actual = File.ReadAllText(task.DestinationFile);
        var expected = File.ReadAllText("expected_results/array.html");

        Assert.True(result);
        Assert.NotNull(actual);
        Assert.NotNull(expected);
        Assert.Equal(expected, actual);
    }
}

public class SimpleItem
{
    public string? Name { get; set; }
}
