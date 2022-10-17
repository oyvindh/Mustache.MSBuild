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
            TeamplateFile = "mustache-templates/simple.mustache",
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
    }
}

public class SimpleItem
{
    public string? Name { get; set; }
}
