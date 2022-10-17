namespace Mustache.MSBuild;

using System.Text.Json;
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
    public void Execute_ValidJsonWithNoSchema_Success()
    {
        var task = new MustacheDirectoryExpand
        {
            BuildEngine = this.buildEngineMock.Object,
            TeamplateFile = "mustache-templates/directory.mustache",
        };

        var result = task.Execute();

        Assert.True(result);
    }

    public void Dispose()
    {
        this.buildEngineMock.VerifyAll();
    }
}