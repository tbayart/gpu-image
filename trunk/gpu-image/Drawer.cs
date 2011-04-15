using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Images {
    /// <summary>
    /// This class is used for drawing 2d digrams, eg of convolutions
    /// </summary>
    class Drawer : Form {

        // bool dodraw = false;

        public Drawer() {
            Paint += (s, e) => Draw();
            Click += (s, e) => Draw();
            Resize += (e, s) => Invalidate();
            Show();
        }

        //void Draw2Xsection() {
        //    if (!dodraw) return;
        //    Form df = this;
        //    int mar = 5; // y margin
        //    df.Show();
        //    Graphics formGraphics = df.CreateGraphics();
        //    // formGraphics.Clear(Conv2DColor.Green);
        //    Pen myPen = new Pen(Conv2DColor.Yellow);
        //    myPen.Width *= 1;
        //    int N = (int)Math.Sqrt(lancv2.Length + 0.5);
        //    float rx = df.ClientSize.Width / (N - 1.0f);
        //    float ry = (df.ClientSize.Height - 2 * mar) / (lancv2.Max() - lancv2.Min());
        //    float my = lancv2.Max() + mar / ry;
        //    float ov = 0;
        //    for (int i = 0; i < N; i++) {
        //        float v = lancv2[i * N + N / 2];
        //        formGraphics.DrawLine(myPen, (i - 1) * rx, (my - ov) * ry, i * rx, (my - v) * ry);
        //        ov = v;
        //    }
        //    myPen.Dispose();
        //    formGraphics.Dispose();
        //}

        public void Draw() {
            List<Conv1D> tlist = drawlist;
            DrawClear();
            foreach (Conv1D c in tlist) Draw(c);
        }

        public void DrawClear() {
            Form df = this;
            Graphics formGraphics = df.CreateGraphics();
            formGraphics.Clear(Color.Green);
            formGraphics.Dispose();
            drawlist = new List<Conv1D>();
        }

        List<Conv1D> drawlist = new List<Conv1D>();
        float drawRange = 4;
        public Color col = Color.Red;
        public void Draw(Conv1D c) {
            // if (!dodraw) return;
            //Draw2Xsection(); return;
            drawlist.Add(c);
            Form df = this;
            int mar = 5; // y margin
            df.Show();
            Graphics formGraphics = df.CreateGraphics();
            // formGraphics.Clear(Conv2DColor.Green);
            Pen myPen = new Pen(col);
            myPen.Width *= 1;

            float ymax = c.Max(); //  1.5f;   // c.Max()
            float ymin = -ymax * 0.5f;  // c.Min()
            float rx = df.ClientSize.Width / (2 * drawRange);
            float ry = (df.ClientSize.Height - 2 * mar) / (ymax - ymin);
            float my = ymax + mar / ry;
            float ov = 0;
            for (float x = c.xlo; x <= c.xhi; x += c.xstep) {
                float v = c[x];
                formGraphics.DrawLine(myPen, (x - c.xstep + drawRange) * rx, (my - ov) * ry, (x + drawRange) * rx, (my - v) * ry);
                ov = v;
            }
            myPen.Dispose();
            formGraphics.Dispose();
            // Draw2Xsection();
        }

        /// <summary>Draw an array, for now assume range is 0..1</summary>
        public void Draw(float[] a) {
            Form df = this;
            int mar = 5; // y margin
            df.Show();
            Graphics formGraphics = df.CreateGraphics();
            // formGraphics.Clear(Conv2DColor.Green);
            Pen myPen = new Pen(col);
            myPen.Width *= 1;

            float ymax = 1; //  a.Max(); //  1.5f;   // c.Max()
            float ymin = 0; //  a.Min();  // c.Min()
            float rx = (float)df.ClientSize.Width / (a.Length);
            float ry = (df.ClientSize.Height - 2 * mar) / (ymax - ymin);
            float my = ymax + mar / ry;
            float ov = 0;
            for (int i = 0; i < a.Length; i++) {
                float v = a[i];
                formGraphics.DrawLine(myPen, (i - 1) * rx, (my - ov) * ry, (i) * rx, (my - v) * ry);
                ov = v;
            }
            myPen.Dispose();
            formGraphics.Dispose();

        }

    }
}
