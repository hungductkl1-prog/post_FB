using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AutoAndroid
{
    internal class UIAutomatorService
    {
        private readonly static string SERVICE_NAME = "uiautomator";
        private readonly static string _path = $"/services/{SERVICE_NAME}";
        private readonly string _url;

        public UIAutomatorService(ATXService atx)
        {
            _url = atx.AtxAgentUrl;
        }

        public bool Start()
        {
            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                var result = socket.HttpPost(_path, null);
                if (result == null || result.Code != 200)
                {
                    return false;
                }
                return true;
            }
        }

        public bool Stop()
        {
            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                var result = socket.HttpDelete(_path);
                if (result == null || result.Code != 200)
                {
                    return false;
                }
                return true;
            }
        }

        public bool Running()
        {
            using (SocketHelper socket = SocketHelper.Create(_url))
            {
                var result = socket.HttpGet(_path);
                if (result == null || result.Code != 200)
                {
                    
                    return false;
                }
                JObject json = JObject.Parse(result.Content);
                return json.Value<bool>("running");
            }
        }

    }
}
