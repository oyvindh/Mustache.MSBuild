namespace Mustache.MSBuild;

using System.Text.Json;

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

    public static Dictionary<string, object> RecursiveDeserialize(Dictionary<string, object> rootDictionary)
    {
        var dictionary = new Dictionary<string, object>(rootDictionary.Count);
        foreach (var keyValuePair in rootDictionary)
        {
            if (keyValuePair.Value is JsonElement element)
            {
                dictionary[keyValuePair.Key] = ParseElement(element);
            }
        }

        return dictionary;
    }

    private static object ParseElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                var list = JsonSerializer.Deserialize<List<object>>(element.ToString());

                if (list != null)
                {
                    var newList = new List<object>(list.Count);

                    foreach (var listElement in list)
                    {
                        if (listElement is JsonElement jsonElement)
                        {
                            newList.Add(ParseElement(jsonElement));
                        }
                        else
                        {
                            newList.Add(listElement);
                        }
                    }

                    return newList;
                }

                throw new InvalidOperationException();

            case JsonValueKind.Object:
                return RecursiveDeserialize(JsonSerializer.Deserialize<Dictionary<string, object>>(element.ToString()) ?? throw new InvalidOperationException());
            default:
                return element;
        }
    }
}
