using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Images {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try {
                Application.Run(new Form1(args));
            } catch (Exception e) {
                MessageBox.Show("cannot run Image.exe. exception " + e);
            }
        }
    }
}
