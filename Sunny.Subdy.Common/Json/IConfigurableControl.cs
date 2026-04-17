using System.Text.Json.Nodes;

namespace Sunny.Subdy.Common.Json
{
    public interface IConfigurableControl
    {
        string Name { get; }
        JsonNode? GetValue();
    }
}
