using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAndroid
{
    internal class ADBException : Exception
    {
        public ADBException(string message) : base(message)
        {
        }
        public ADBException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }


}
