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
    public string TemplateFile { get; set; } = "template.mustache";

    [Required]
    public string DataFile { get; set; } = "data.json";

    [Required]
    public string DestinationFile { get; set; } = "expanded.txt";

    public string InputData { get; set; } = string.Empty;

    public override bool Execute()
    {
        var templateString = File.ReadAllText(this.TemplateFile);
        using var dataStream = File.OpenRead(this.DataFile);
        var rootDict =
            InputParser.RecursiveDeserialize(JsonSerializer.Deserialize<Dictionary<string, object>>(dataStream) ?? throw new InvalidOperationException());
        var inputDictionary = InputParser.Parse(this.InputData);

        var mergedDict = new[] { rootDict, inputDictionary }
            .SelectMany(dict => dict)
            .ToLookup(pair => pair.Key, pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.Last());
        var expandedTemplate = Template.Compile(templateString).Render(mergedDict);

        File.WriteAllText(this.DestinationFile, expandedTemplate);
        return true;
    }
}
