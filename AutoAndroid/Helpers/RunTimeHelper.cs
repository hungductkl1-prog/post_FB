using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class RunTimeHelper
    {
        public static void Run(Action action)
        {
            Run("RunTimeHelper", action);
        }

        public static void Run(string name, Action action)
        {
            var watch = Stopwatch.StartNew();
            action.Invoke();
            watch.Stop();
        }

        public static T Run<T>(Func<T> action)
        {
            return Run("RunTimeHelper", action);
        }

        public static T Run<T>(string name, Func<T> action)
        {
            var watch = Stopwatch.StartNew();
            T value = action.Invoke();
            watch.Stop();
            return value;
        }

        public static T Time<T>(Func<T> action, out int usetime)
        {
            var watch = Stopwatch.StartNew();
            T value = action.Invoke();
            watch.Stop();
            usetime = (int)watch.Elapsed.TotalMilliseconds;
            return value;
        }
        public static void Time(Action action, out int usetime)
        {
            var watch = Stopwatch.StartNew();
            action.Invoke();
            watch.Stop();
            usetime = (int)watch.Elapsed.TotalMilliseconds;
            return;
        }
    }
}
