using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Sunny.Subdy.Common.Json
{
    public interface IControlAdapter
    {
        string Name { get; }
        void LoadValue(JToken value);
        void BindEvent(EventHandler handler);
    }
}
