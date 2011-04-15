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


    // string extensions
    public static class SX {
        public static double Double(this String s) {
            try {
                if (s == "") return 0;
                return double.Parse(s);
            } catch (Exception e) {
                Form1.Beep(e);
                return 0;
            }
        }
        public static float Float(this String s) {
            try {
                if (s == "") return 0;
                return float.Parse(s);
            } catch (Exception e) {
                Form1.Beep(e);
                return 0;
            }
        }
        public static int Int(this String s) {
            try {
                if (s == "") return 0;
                return int.Parse(s);
            } catch (Exception e) {
                Form1.Beep(e);
                return 0;
            }
        }
    }


}
