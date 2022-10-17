namespace Mustache.MSBuild;

using Microsoft.Build.Framework;
using Mustache;
using System.Text.Json;

/// <summary>
/// Task that expand templates using Mustache.
/// </summary>
public class MustacheExpand : Microsoft.Build.Utilities.Task
{
    [Required]
    public string TeamplateFile { get; set; } = "template.mustache";

    [Required]
    public string DataFile { get; set; } = "data.json";

    [Required]
    public string DestinationFile { get; set; } = "expanded.txt";

    public override bool Execute()
    {
        var templateString = File.ReadAllText(this.TeamplateFile);
        using var dataStream = File.OpenRead(this.DataFile);
        var dataObject = JsonSerializer.Deserialize<Dictionary<string, object>>(dataStream);
        var expandedTemplate = Template.Compile(templateString).Render(dataObject);

        File.WriteAllText(this.DestinationFile, expandedTemplate);
        return true;
    }
}