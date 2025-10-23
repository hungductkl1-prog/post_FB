using AutoAndroid;
using Sunny.Subd.Core.Models;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunny.Subd.Core.Facebook.ScriptActions
{
    public interface IActionHandler
    {
        string TypeAction { get; }
         Task<SubdyExtension> ExecuteAsync(Account account, ADBClient device, JsonHelper settingScript, JsonHelper settingAction, JsonHelper settingGeneral);
    }

}
