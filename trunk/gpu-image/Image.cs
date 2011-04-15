using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.ComponentModel;
// using System.Data;
using System.Drawing;

using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
// using TraceNamespace;  //
// Issues: colour if no exif info to say srgb
// aspect ration (and content) if >8192

namespace Images {

    public enum NextOp { STD, ALT, SHARE, NEW }

    /// <summary>
    /// The ImageTex class is the main class for holding reference to an image stored as a texture.
    /// This class may be used for writing standard C# programs on images, or as support for the interactive display code.
    /// 
    /// The image held as texture is no longer valid after DeviceLost.  
    /// TODO: add a check for attempt to use images in this way?
    /// </summary>
    public class ImageTex {
        public Texture2D texture;  // holds the image in device form
        public string fid;
        public string fileInfo;
        public NextOp nextOpNew = NextOp.STD;  // 0 use std, 1 alt, 2 from share, 3 new
        /// <summary>step in image; 1/size in each dimension</summary> 
        internal Vector2 stepImageXy {
            get {
                return new Vector2(1f / texture.Width, 1f / texture.Height);
            }
        }

        /// <summary>four convenience images named for easy use from program console</summary> 
        public static ImageTex i1, i2, i3, i4;

        private Effects effects;  // the Effects control associated with this ImageTex
        //public static void DE(string name, string code) { _.Effects.CreateEffectDirect(name, code); }

        //public ImageTex() { }  // only used for interpreter to get at statics
        public ImageTex(string fid) : this(fid, Form1._.Effects) { }

        /// <summary>create an ImageDef valid for the given Effects</summary> 
        public ImageTex(Effects effects) {
            this.effects = effects;
        }

        /// <summary>load an image file as ImageDef valid for the given Effects</summary> 
        public ImageTex(string fid, Effects effects) {
            this.fid = fid;

            try {
                FileInfo fi = new FileInfo(fid);

                texture = Texture2D.FromFile(effects.graphicsDevice, fid); // , tcp);
                if (texture.Width == 8192 || texture.Height == 8192) {
                    Size size = ImageHelpers.GetDimensions(fid);
                    if (size.Width > 8192 || size.Height > 8192) {
                        MessageBox.Show("aspect ration may be wrong real size " + size.Width + "x" + size.Height);
                    }
                }
                fileInfo = string.Format(" {0} {1}x{2}   {3}bytes {4}", fi.FullName, texture.Width, texture.Height, fi.Length, fi.CreationTime);
            }
            catch (Exception e) {
                MessageBox.Show("Cannot load image " + fid + ": " + e);
                throw e;
            }
        }

        /// <summary>associate a texture with an image fid</summary> 
        public ImageTex(string fid, Texture2D texture, Effects effects, string fileInfo) {
            this.fid = fid;
            this.effects = effects;
            this.texture = texture;
            this.fileInfo = fileInfo;
        }

        public ImageTex(ImageTex i) : this(i.fid, i.texture, i.effects, i.fileInfo) { }

        public int Width { get { return texture.Width; } }
        public int Height { get { return texture.Height; } }

        /// <summary>add two images (pointwise)</summary>
        public static ImageTex operator +(ImageTex a, ImageTex b) { return a.effects.Xrender(new ImageTex[] { a, b }, "plus"); }
        /// <summary>subtract images (pointwise)</summary>
        public static ImageTex operator -(ImageTex a, ImageTex b) { return a.effects.Xrender(new ImageTex[] { a, b }, "minus"); }
        /// <summary>multiply images (pointwise)</summary>
        public static ImageTex operator *(ImageTex a, ImageTex b) { return a.effects.Xrender(new ImageTex[] { a, b }, "times"); }
        /// <summary>divide first image by second (pointwise)</summary>
        public static ImageTex operator /(ImageTex a, ImageTex b) { return a.effects.Xrender(new ImageTex[] { a, b }, "divide"); }
        /// <summary>Min of two images (pointwise)</summary>
        public ImageTex Min(ImageTex b) { return effects.Xrender(new ImageTex[] { this, b }, "min"); }
        /// <summary>Max of two images (pointwise)</summary>
        public ImageTex Max(ImageTex b) { return effects.Xrender(new ImageTex[] { this, b }, "max"); }


        /// <summary>(inefficient) scalar multiply of image</summary>
        public static ImageTex operator *(ImageTex a, float fa) { return a.Mad(fa); }

        /// <summary>(inefficient) scalar multiply of image</summary>
        public ImageTex Mad(float fa) {
            return effects.Xrender(new ImageTex[] { this }, "mad", new float[] { 1, fa, 0, 0, 0 });
        }
        /// <summary>multiply and add two images</summary>
        public ImageTex Mad(ImageTex b, float fa, float fb) {
            return effects.Xrender(new ImageTex[] { this, b }, "mad", new float[] { 2, fa, fb, 0, 0 });
        }
        /// <summary>multiply and add three images</summary>
        public ImageTex Mad(ImageTex b, ImageTex c, float fa, float fb, float fc) {
            return effects.Xrender(new ImageTex[] { this, b, c }, "mad", new float[] { 3, fa, fb, fc, 0 });
        }
        /// <summary>multiply and add four images</summary>
        public ImageTex Mad(ImageTex b, ImageTex c, ImageTex d, float fa, float fb, float fc, float fd) {
            return effects.Xrender(new ImageTex[] { this, b, c, d }, "mad", new float[] { 4, fa, fb, fc, fd });
        }

        /// <summary>transform an image using 4*4 transform (??? check what happens about perspective???)</summary>
        public ImageTex Tran(Matrix tran) {
            return effects.Xrender(new ImageTex[] { this }, "bilin", null, tran);
        }
        /// <summary>Rotate and image clockwise by given number of degrees</summary>
        public ImageTex Rot(float r) {
            return Tran(Matrix.CreateRotationZ(MathHelper.ToRadians(r)));
        }

        /// <summary>Apply 3x3 median filter to an image</summary>
        public ImageTex Med3() { return effects.Xrender(this, "med"); }
        /// <summary>Apply fixed 3x3 sharp mask to an image</summary>
        public ImageTex Sharp() { return effects.Xrender(this, "conv", new float[] { 12, 1,1,1, 0, -1, 0, -1, 5, -1, 0, -1, 0 }); }
        /// <summary>Apply fixed 3x3 blur mask to an image</summary>
        public ImageTex Blur() { return effects.Xrender(this, "conv", new float[] { 12, 1,1,1, 1f/9, 1f/9, 1f/9, 1f/9, 1f/9, 1f/9, 1f/9, 1f/9, 1f/9 }); }

        /// <summary>work out Gaussian filter TODO: private/static</summary>
        float[] Gauss(double r) {
            // e**-(r**2/c**2) = 0.05  => -r**2/c**2 = ln(0.05)  => c = r*sqrt(-ln(0.05))
            if (r < 0) r = 0;
            double c2 = r * r / -Math.Log(0.05);
            int K = (int)Math.Ceiling(r);
            float[] p = new float[(2 * K + 1)];
            int i = 0;
            for (int x = -K; x <= K; x++) {
                double v = Math.Exp(-(x * x) / c2);
                p[i++] = (float)v;
            }
            if (r == 0) { p[0] = 1; }
            return p;
        }

        /// <summary>Apply gaussian blur of image:  r is radius, allow drop to 0.05 at r</summary>
        public ImageTex Blur(double r) {
            if (r < 0) r = 0;
            int K = (int)Math.Ceiling(r);
            float[] gauss = Gauss(r);
            float[] p = new float[4].Concat(gauss).ToArray();
            p[0] = p.Length;
            p[1] = K; p[2] = 0;
            p[3] = (float) gauss.Sum();
            ImageTex a = effects.Xrender(this, "conv", p);
            p[1] = 0; p[2] = K;
            return effects.Xrender(a, "conv", p);
        }

        /// <summary>Apply unsharp mask to image, with given radius, strength and threshold</summary>
        public ImageTex Unsharp(double r, double strength, double threshold) {
            int K = (int)Math.Ceiling(r);
            float[] gauss = Gauss(r);
            float[] p = new float[3].Concat(gauss).ToArray();
            p[0] = p.Length;
            p[1] = (float)strength;
            p[2] = (float)threshold;
            ImageTex a = effects.Xrender(this, "Unsharp_" + K, p);
            return a;
        }


        /// <summary>image processing render pass of technique name without parameters on entire image, return new image</summary>
        public ImageTex Xrender(String name) {
            return Xrender(name, null);
        }

        /// <summary>image processing render pass of technique name with given parameters on entire image, return new image</summary>
        public ImageTex Xrender(String name, float[] parms) {
            return Xrender(name, parms);
        }

        /// <summary>Display the given image (in default form)</summary>
        public void Show() { Form1.ShowImageDefault(this); }
        /// <summary>Display the given image (in default form), and return itself</summary>
        public ImageTex s { get { Form1.ShowImageDefault(this); return this; } }
        /// <summary>Display the given image (in a tiny corner on default form), and return itself.  Mainly for timing tests.</summary>
        public ImageTex ss { get { Form1.ShowImageDefaultSmall(this); return this; } }
        /// <summary>Control of pipeline, ALT, result is to be put into alternate RenderTarget xtarget1</summary>
        public ImageTex q { get { ImageTex r = new ImageTex(this); r.nextOpNew = NextOp.ALT; return r; } }
        /// <summary>Control of pipeline, SHARE: result is to be put into next RenderTarget in the share list (reuse arbitrary but enough for experiment)</summary>
        public ImageTex qq { get { ImageTex r = new ImageTex(this); r.nextOpNew = NextOp.SHARE; return r; } }
        /// <summary>Control of pipeline, NEW: mane a new RederTarget to hold the result</summary>
        public ImageTex qqq { get { ImageTex r = new ImageTex(this); r.nextOpNew = NextOp.NEW; return r; } }

        /// <summary>Perform 3x3 median filter on image</summary>
        public ImageTex median() { return Xrender("med"); }
        /// <summary>Perform 3x3 median filter on image</summary>
        public ImageTex med { get { return Xrender("med"); } }
        /// <summary>Perform direct copy of image, mainly used for timing tests</summary>
        public ImageTex d { get { return Xrender("dir"); } }
        /// <summary>Perform n sequential direct copies of image, mainly used for timing tests</summary>
        public ImageTex dd(int n) {
            ImageTex r = this;
            for (int i = 0; i < n; i++)
                r = r.Xrender("dir");
            return r;
        }

        /// <summary>get real byte data for an ImageTex</summary>
        public byte[] data {
            get {
                byte[] timdata = new byte[texture.Height * texture.Width * 4];
                texture.GetData<byte>(timdata);
                return timdata;
            }
        }

    }
}
