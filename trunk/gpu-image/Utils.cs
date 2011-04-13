// bits taken from SystemHelpers
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;


namespace Images {
    class Utils {
        [DllImport("user32.dll")]
        public static extern ushort GetAsyncKeyState(uint vKey);
        public const uint VK_LCONTROL = 0xA2;
        public const uint VK_RCONTROL = 0xA3;
        public const uint VK_ESCAPE = 0x1B;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern string GetCommandLineA();

    }
}
