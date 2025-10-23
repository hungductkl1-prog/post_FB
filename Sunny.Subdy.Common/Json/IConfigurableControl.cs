using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunny.Subdy.Common.Json
{
    public interface IConfigurableControl
    {
        string Name { get; }
        object? GetValue();
    }
}
