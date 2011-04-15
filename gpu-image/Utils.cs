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
                return double.Parse(s.Post("="));
            } catch (Exception e) {
                Form1.Beep(e);
                return 0;
            }
        }
        public static float Float(this String s) {
            try {
                if (s == "") return 0;
                return float.Parse(s.Post("="));
            } catch (Exception e) {
                Form1.Beep(e);
                return 0;
            }
        }
        public static int Int(this String s) {
            try {
                if (s == "") return 0;
                return int.Parse(s.Post("="));
            } catch (Exception e) {
                Form1.Beep(e);
                return 0;
            }
        }

        /// <summary> if split is in s, return the part before split.  else return s</summary>
        public static String Pre(this string s, string split) {
            int i = s.IndexOf(split);
            if (i == -1) return s;
            return s.Substring(0, i);
        }

        /// <summary> if split is in s, return the part after split.  else return s</summary>
        public static String Post(this string s, string split) {
            int i = s.IndexOf(split);
            if (i == -1) return s;
            return s.Substring(i + split.Length);
        }
    }


}
