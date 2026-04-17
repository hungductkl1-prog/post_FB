using System.Text.Json.Nodes;

namespace Sunny.Subdy.Common.Json
{
    public interface IControlAdapter
    {
        string Name { get; }
        void LoadValue(JsonNode? value);
        void BindEvent(EventHandler handler);
    }
}
