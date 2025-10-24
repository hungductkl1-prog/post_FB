using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamAndroid.Services
{
    public static class KeycodeHelper
    {
        private static readonly Dictionary<Keys, AndroidKeycode> keycodeDict = new Dictionary<Keys, AndroidKeycode>
        {
            { Keys.Space, AndroidKeycode.AKEYCODE_SPACE },
            { Keys.Back, AndroidKeycode.AKEYCODE_DEL },
            { Keys.Left, AndroidKeycode.AKEYCODE_DPAD_LEFT },
            { Keys.Up, AndroidKeycode.AKEYCODE_DPAD_UP },
            { Keys.Right, AndroidKeycode.AKEYCODE_DPAD_RIGHT },
            { Keys.Down, AndroidKeycode.AKEYCODE_DPAD_DOWN },
            { Keys.Delete, AndroidKeycode.AKEYCODE_FORWARD_DEL },
            { Keys.Tab, AndroidKeycode.AKEYCODE_TAB },
            { Keys.Enter, AndroidKeycode.AKEYCODE_ENTER },
        };

        public static AndroidKeycode ConvertKey(Keys key)
        {
            // A - Z
            if (key >= Keys.A && key <= Keys.Z)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_A - (int)Keys.A;
                return (AndroidKeycode)((int)key + offset);
            }
            // Digits 0-9
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_0 - (int)Keys.D0;
                return (AndroidKeycode)((int)key + offset);
            }
            else if (keycodeDict.TryGetValue(key, out var androidKey))
            {
                return androidKey;
            }

            return AndroidKeycode.AKEYCODE_UNKNOWN;
        }

        public static AndroidMetastate ConvertModifiers(Keys keyModifiers)
        {
            AndroidMetastate metastate = AndroidMetastate.AMETA_NONE;

            if (keyModifiers.HasFlag(Keys.Shift))
                metastate |= AndroidMetastate.AMETA_SHIFT_ON;

            if (keyModifiers.HasFlag(Keys.Control))
                metastate |= AndroidMetastate.AMETA_CTRL_ON;

            if (keyModifiers.HasFlag(Keys.Alt))
                metastate |= AndroidMetastate.AMETA_ALT_ON;

            return metastate;
        }
    }
}
