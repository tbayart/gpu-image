using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Text.RegularExpressions;
using System.IO;


namespace Images {
    /// <summary>class to manage convolution code</summary>
    public class Convolutions {

        /// <summary>set up a 3d convolution for a Lanczos filter, using the given values SQWidth etc, and draw using form1</summary>
        static internal Conv2D MakeLanczos(Form1 form1, int a, double LancW, double SQWidth, int K) {
            //toggle = !toggle; if (toggle) return MakeLanczosOld(a, K);
            if (K == 0) K = 16;  // default
            int s = (int)Math.Ceiling(Math.Log(2 * a * K, 2));
            int N = (int)Math.Round(Math.Pow(2, s));  // actual size of texture
            float LSQWidth = (float)(Math.Max(0.001f, SQWidth)); // K * 8; //  K * 4;

            Lanc1D lanc = new Lanc1D(a, K * 20);
            lanc.SetRange((float)LancW);
            SQ1D sq = new SQ1D(LSQWidth);
            Conv1D lancsq = lanc * sq;
            Conv2D lanczos2d = lancsq.MakeTexture(form1.graphicsDevice, 64);

            form1.Draw(lancsq);
            return lanczos2d;
        }
    }

    /// <summary>class to hold a 2d convolution, with main data held in a texture</summary> 
    public class Conv2D {
        public int K;        // width in array terms (length -1)
        public float xlo;   // value of x represented by v[0]
        public float xhi;  // value of x represented by v[K]
        public float xstep;   // difference of value of x between v[n] and v[n+1]
        public float xwidth;  // range of values of x
        public Texture2D texture;

        public Conv2D(Conv1D c1, Texture2D ptexture) {
            xlo = c1.xlo;
            xhi = c1.xhi;
            xstep = c1.xstep;
            xwidth = c1.xwidth;
            texture = ptexture;
        }
    }

    /// <summary>class to hold a 1d convolution, with main data held in an array, and also extra metadata</summary> 
    public class Conv1D {
        public float[] v;    // real data
        public int K;        // width in array terms (length -1)
        public float xlo;   // value of x represented by v[0]
        public float xhi;  // value of x represented by v[K]
        public float xstep;   // difference of value of x between v[n] and v[n+1]
        public float xwidth;  // range of values of x

        // convolution representing range xlow .. xhigh
        public Conv1D(float[] a, float xrange) : this(a, -xrange, xrange) { }
        public Conv1D(float[] a, float pxlo, float pxhi) {
            this.v = a;
            K = a.Length - 1;
            SetRange(pxlo, pxhi);
        }
        public Conv1D(int N, float xrange) : this(new float[N], -xrange, xrange) { }
        public Conv1D(int N, float pxlo, float pxhi) : this(new float[N], pxlo, pxhi) { }

        public int Length { get { return v.Length; } }
        // retrospectively set range.
        // should not generally be used except by constructor,
        // but sometimes useful to save definition of lots of constructors
        public void SetRange(float pxlo, float pxhi) {
            xlo = pxlo;
            xhi = pxhi;
            xwidth = xhi - xlo;
            xstep = xwidth / K;
        }
        public void SetRange(float r) { SetRange(-r, r); }

        // -1..1 range for x in K2
        float P(float i, float K2) {
            float p = ((i + 0.5f) * 2 / K2 - 1) * xwidth / 2;  // TODO: allow for non-centred
            return p;
        }

        //// p in -1 .. 1, return p in 0..K
        //float I(float p, int K) {
        //    float i = (p + 1) * K / 2 - 0.5f;
        //    return i;
        //}

        /// <summary>convolve two 1d convolutions</summary> 
        public static Conv1D operator *(Conv1D ca, Conv1D cb) {
            // conserve same spacing as ca
            int N = (int)Math.Round((ca.xwidth + cb.xwidth) / ca.xstep);
            Conv1D cc = new Conv1D(N, (ca.xwidth + cb.xwidth) / 2);
            float[] rv = cc.v;
            int i = 0;
            for (float rx = cc.xlo; rx <= cc.xhi; rx += cc.xstep) {  // rx position in output
                for (float bx = cb.xlo; bx <= cb.xhi; bx += ca.xstep) {  // bx position in b
                    rv[i] += ca[rx - bx] * cb[bx];
                }
                i++;
            }
            return cc;
        }

        /// <summary>max value in convolution array</summary> 
        public float Max() { return v.Max(); }
        /// <summary>min value in convolution array</summary> 
        public float Min() { return v.Min(); }

        /// <summary>extract interpolated value for point in convolution array</summary> 
        public float this[float x] {
            get {
                if (x < xlo) return 0;
                if (x > xhi) return 0;
                float rx = (x - xlo) / xstep;
                int ix = (int)Math.Floor(rx);
                float dx = rx - ix;
                float rv = v[ix] * (1 - dx) + (dx == 0 ? dx : v[ix + 1] * dx);
                return rv;
            }
        }

        /// <summary>make a 2d convolution (with texture) from a 1d convolution (with array)</summary> 
        public Conv2D MakeTexture(GraphicsDevice graphicsDevice, int K) {
            float[,] v2d = Circle2D(K, Form1.CircP);
            return MakeTexture(graphicsDevice, v2d);
        }

        private Conv2D MakeTexture(GraphicsDevice graphicsDevice, float[,] a) {
            int M = a.GetLength(0);
            int N = a.GetLength(1);
            Texture2D t = new Texture2D(graphicsDevice, N, M, 1, TextureUsage.Linear, SurfaceFormat.Single);
            t.SetData<float>(Flatten(a));
            return new Conv2D(this, t);
        }

        /// <summary>make a 1d array by flattening a 2d array</summary>
        private static float[] Flatten(float[,] a) {
            int M = a.GetLength(0);
            int N = a.GetLength(1);
            float[] r = new float[M * N];
            int p = 0;
            for (int i = 0; i < M; i++)
                for (int j = 0; j < N; j++)
                    r[p++] = a[i, j];
            return r;
        }


        /// <summary>take a 1d conv and make it 2d by sweeping into a super-egg
        /// note: only +ve part of input conv will be used
        /// result will be array size K2xK2, with egg power p
        ///</summary>
        float[,] Circle2D(int K2, double p) {
            float[,] rv = new float[K2, K2];
            for (int i = 0; i < K2; i++) {
                for (int j = 0; j < K2; j++) {
                    if (i == (int)(K2 / 2) && j == (int)(K2 / 2)) { }
                    float ii = Math.Abs(P(i, K2));
                    float jj = Math.Abs(P(j, K2));
                    float rr = (float)Math.Pow(Math.Pow(ii, p) + Math.Pow(jj, p), 1 / p);
                    rv[i, j] = this[rr];
                }
            }
            return rv;
        }

    }

    /// <summary>Lanczos 1d convolution class</summary>
    class Lanc1D : Conv1D {
        // 1d Lanczos
        /// <summary>
        /// create a 1d Lanczos filter with given a (number harmonics), using a given numbner of points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="N"></param>
        public Lanc1D(int a, int N)
            : base(N, a) {
            float[] rv = v;

            float pi = MathHelper.Pi;

            int p = 0;
            for (float i = 0; i < N; i++) {
                double x = 2 * (i + 0.5) * a / N - a;
                // double yr = y == 0 ? 1 : (a * Math.Sin(pi * y) * Math.Sin(pi * y / a) / (pi * pi * y * y));
                double rr = x == 0 ? 1 : x > a ? 0 : (a * Math.Sin(pi * x) * Math.Sin(pi * x / a) / (pi * pi * x * x));
                rv[p++] = (float)(rr);
            }
            // return rv;
        }
    }

    /// <summary>square wave 1d convolution class</summary>
    class SQ1D : Conv1D {
        public SQ1D(float range)
            : base(2, range) {
            for (int i = 0; i < v.Length; i++) v[i] = 1f / (K + 1);
        }
    }

}

/**** unsharp mask experimental values from PSP8
 * input image, bg 64, point 192
 *    r   s        x  1   1.4  2  2.2     x  1   1.4  2  2.2 
 *    1.0 50      56  8   3             246  58  61
 * 
 *  */

