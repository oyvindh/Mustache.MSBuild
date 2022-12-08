namespace Mustache.MSBuild;

internal class InputParser
{
    public static IDictionary<string, object> Parse(string inputData)
    {
        if (string.IsNullOrEmpty(inputData))
        {
            return new Dictionary<string, object>();
        }

        var dict = new Dictionary<string, object>();
        var data = inputData.Split(';');
        foreach (var item in data)
        {
            var keyValue = item.Split('=');
            if (keyValue.Length != 2)
            {
                throw new InvalidDataException($"Invalid input data, '{item}'.");
            }

            dict.Add(keyValue[0], keyValue[1]);
        }

        return dict;
    }
}
