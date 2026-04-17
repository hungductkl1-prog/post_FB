using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAndroid
{
    public class AtxTouch
    {
        private readonly ATXService _atx;
        private int _lastX;
        private int _lastY;

        internal AtxTouch(ATXService atx)
        {
            _atx = atx;
        }

        public AtxTouch Down(int x, int y)
        {
            _lastX = x;
            _lastY = y;
            Event(0, x, y);
            return this;
        }

        public AtxTouch Up(int x, int y)
        {
            Event(1, x, y);
            return this;
        }

        public AtxTouch Move(int x, int y)
        {
            _lastX = x;
            _lastY = y;
            Event(2, x, y);
            return this;
        }

        public AtxTouch Wait(int wait)
        {
            // Gửi Move events liên tục để Android nhận diện long press
            int elapsed = 0;
            const int interval = 50;
            while (elapsed < wait)
            {
                Thread.Sleep(interval);
                elapsed += interval;
                Event(2, _lastX, _lastY);
            }
            return this;
        }

        private void Event(int @event, int x, int y)
        {
            _ = _atx.JsonRpc("injectInputEvent", @event, x, y, 0);
        }

        public static AtxTouch Down(ATXService atx, int x, int y)
        {
            return new AtxTouch(atx).Down(x, y);
        }

        public static AtxTouch Up(ATXService atx, int x, int y)
        {
            return new AtxTouch(atx).Up(x, y);
        }

        public static AtxTouch Move(ATXService atx, int x, int y)
        {
            return new AtxTouch(atx).Move(x, y);
        }
    }
}
