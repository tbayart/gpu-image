using System;
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
    /// <summary>class to manage effects for a given graphics device</summary>
    public class Effects {

        /// <summary>file containing the core effects</summary>
        private static string fxfile = @"Content\image.fx";

        /// <summary>graphics device for which the effects are prepared</summary>
        internal GraphicsDevice graphicsDevice;

        /// <summary>the main compiled effects (from fxfile), from which techniques may be extracted</summary>
        Effect mainEffect;

        /// <summary>dictionary of all the techniques we have so far (???static and dynamic)</summary>
        Dictionary<string, FullTechnique> dynamicEffects = new Dictionary<string, FullTechnique>();

        /// <summary>two prepared render targets</summary> 
        RenderTarget2D xtarget, xtarget1;

        /// <summary>construct an Effects object for a given GraphicsDevice</summary>
        public Effects(GraphicsDevice graphicsDevice) { this.graphicsDevice = graphicsDevice; }

        /// <summary>initialize the services and get the effect compiled and ready</summary>
        public void LoadContentRuntime() {
            mainEffect = LoadContentRuntimeFid(fxfile);
        }

        public static bool useVector4 = false;

        /// <summary>compile and load an effect from a file (runtime compilation of file)</summary>
        private Effect LoadContentRuntimeFid(string fid) {
            Effect reffect = null;
            try {
                // ForcePixelShaderSoftwareNoOptimizations gives wrong answers
                // SkipOptimizations makes the variable size convolution work, but at what cost??
                CompilerOptions copts = CompilerOptions.SkipOptimization; // | CompilerOptions.AvoidFlowControl;
                CompiledEffect compiledEffect = Effect.CompileEffectFromFile(fid, null, null, copts, TargetPlatform.Windows);

                if (compiledEffect.Success) {
                    EffectPool effectPool = new EffectPool();
                    reffect = new Effect(graphicsDevice, compiledEffect.GetEffectCode(), CompilerOptions.None, effectPool);
                }
                else {
                    System.Windows.Forms.MessageBox.Show("Error in shader '" + fid + "'.  Previous version will be used.\n\n" + compiledEffect.ErrorsAndWarnings);
                }
            }
            catch (Exception e) {
                System.Windows.Forms.MessageBox.Show("Unexpected error in shader '" + fid + "'.  Previous version will be used.\n\n" + e);
            }
            return reffect;
        }

        /// <summary>compile and load an effect from a string containing effect code</summary>
        private Effect LoadContentRuntimeString(string content, string name) {
            Effect reffect = null;
            try {
                // ForcePixelShaderSoftwareNoOptimizations gives wrong answers
                // SkipOptimizations makes the variable size convolution work, but at what cost??
                CompilerOptions copts = CompilerOptions.SkipOptimization; // | CompilerOptions.AvoidFlowControl;
                CompiledEffect compiledEffect = Effect.CompileEffectFromSource(content, null, null, copts, TargetPlatform.Windows);

                if (compiledEffect.Success) {
                    EffectPool effectPool = new EffectPool();
                    reffect = new Effect(graphicsDevice, compiledEffect.GetEffectCode(), CompilerOptions.None, effectPool);
                }
                else {
                    System.Windows.Forms.MessageBox.Show("Error in dynamic shader " + name +".  Previous version will be used.\n\n" + compiledEffect.ErrorsAndWarnings);
                }
            }
            catch (Exception e) {
                System.Windows.Forms.MessageBox.Show("Unexpected error in dynamic shader " + name + ".  Previous version will be used.\n\n" + e);
            }
            return reffect;
        }

        ///// <summary>Create an effect from a string, using a file as a temporary staging post (only used for some time tests)</summary>
        //internal Effect CreateEffectViaFile(string name, string s) {
        //    string fid = "temp.fx";
        //    string c = CreateFullEffectCode(s);
        //    File.WriteAllText(fid, c, Encoding.ASCII);
        //    Effect effect = LoadContentRuntimeFid(fid);
        //    dynamicEffects[name] = new FullTechnique(effect, effect.Techniques["Standard"]);
        //    return effect;
        //}

        /// <summary>Create a named technique from the "Standard" technique in an effect string </summary>
        private FullTechnique CreateEffectDirectI(string name, string s) {
            string c = CreateFullEffectCode(s);
            Effect effect = LoadContentRuntimeString(c, name);
            if (effect == null) return null;
            FullTechnique ft = new FullTechnique(effect, effect.Techniques["Standard"], name);
            return ft;
        }

        /// <summary>
        /// Create a dynamic effect and apply directly (do not save to file)
        /// </summary>
        /// <param name="name">name for the effect</param>
        /// <param name="s">string code for the effect</param>
        /// <param name="sub">substitution parameters; pairs of name and value</param>
        /// <returns></returns>
        public FullTechnique CreateEffectDirect(string name, string s, params string[] sub) {
            for (int i = 0; i < sub.Length; i += 2) s = s.Replace("$" + sub[i] + "$", sub[i + 1]);
            return CreateEffectDirectI(name, s);
        }


        /// <summary>StringBuilder convenience for collecting a dynamic effect string</summary>
        StringBuilder sb;
        /// <summary>accumulate m into a string: convenience for collecting a dynamic effect string</summary>
        private void o(string m) { sb.Append(m + "\n"); }

        /// <summary>Dynamically generate effect for k x l percentile.  Must be code because v_n is effectively written array.</summary>
        private FullTechnique PercentileTest(int k, int l) {
            sb = new StringBuilder(); // DynamicFx.BubbleStart);
            o("float4 rgb;");
            o("float4 myrgb = tex2D(is, Input.TexPos);");
            o("float myl = max(myrgb.r, max(myrgb.g, myrgb.b));");
            // collect the data
            int N = 0;
            for (int x = -k; x <= k; x++) {
                for (int y = -l; y <= l; y++) {
                    o("rgb = tex2D(is, Input.TexPos + float2(" + x + "," + y + ")*stepImageXy);");
                    o("float v_" + N + " = max(rgb.r, max(rgb.g, rgb.b));");
                    N++;
                }
            }

            // find nth lowest, parms[1] is percentile as fraction (0..1)
            o("float N = min(1,parms[1])*"+N+";");
            o("int bestless = 9999; float bestv =99999; int less=0;");
            for (int i = 0; i < N; i++) {
                o("less = 0;");
                for (int j = 0; j < N; j++) {
                    // this compiles for percentile2 but does not run
                    // o("less += (v_" + i + " >= v_" + j + ") ? 1 : 0;");
                    o("if (v_" + i + " >= v_" + j + ") less++;");
                }
                o("if (N <= less && less <= bestless) { bestless = less; bestv = v_"+i+"; }" );
            }

            // set output
            o("col = myrgb * bestv / myl;");
            o("col.a = 1;");

            // Console.WriteLine("--------------------------------\n" + sb);
            String name = k == l ? "Percentile_" + k : "Percentile" + k + "_" + l;
            return CreateEffectDirect(name, sb.ToString());
        }

        /// <summary>Dynamically generate effect for k-way median filter.  Must be code because v_n is effectively written array.</summary>
        private FullTechnique MedianTest(int k, int l) {
            sb = new StringBuilder(); // DynamicFx.BubbleStart);
            o("float4 rgb; float l; float swap2;");
            o("float4 myrgb = tex2D(is, Input.TexPos);");
            o("float myl = max(myrgb.r, max(myrgb.g, myrgb.b));");
            // collect the data
            int N = 0;
            for (int x = -k; x <= k; x++) {
                for (int y = -l; y <= l; y++) {
                    o("rgb = tex2D(is, Input.TexPos + float2(" + x + "," + y + ")*stepImageXy);");
                    o("l = max(rgb.r, max(rgb.g, rgb.b));");
                    o("float v_" + N + " = l;");
                    N++;
                }
            }

            // sort the data ~ bubble sort .. ow
            for (int i = 0; i < N; i++) {
                for (int j = i + 1; j < N; j++) {
                    o("if (v_" + i + " > v_" + j + ") { float swap = v_" + i + "; v_" + i + " = v_" + j + "; v_" + j + " = swap; }");
                }
            }

            // set output
            o("float m = v_" + (N / 2) + ";");
            o("col = myrgb * m / myl;");
            o("col.a = 1;");

            // Console.WriteLine("--------------------------------\n" + sb);
            return CreateEffectDirect("Median_" + k + "_" + l, sb.ToString());
        }


        /// <summary>Create full compilable effect code from 'core' effect code "DynamicFx.Framework"</summary>
        public static string CreateFullEffectCode(string s) {
            string ss = Regex.Replace(s, @"i\[(.*?),(.*?)\]", @"tex2D(is, Input.TexPos + float2(($1), ($2))*stepImageXy)");
            ss = ss.Replace("ii0", "tex2D(is, Input.TexPos)");

            string c = DynamicFx.Framework;
            c = c.Replace("%CODE%", ss);

            //Regex arr = new Regex(@"i\[(.*),(.*)\]");

            //Match m = arr.Replace(c, 
            return c;
        }
        /* test  
for(int i=0; i<100;i++) { DE("q", "float p1=parms[1]; float p2=parms[2];col=(ii0+i[p1,p2])/2;"); i1.Xrender("q", new float[]{2,i,2*i}).Show(); }
         * */

        /// <summary>
        /// Get the technique given a possibly abbbreviated name, and apply parms
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FullTechnique GetTechnique(string name, float[] parms) {
            name = name.ToLower();
            FullTechnique r = null;
            if (dynamicEffects.ContainsKey(name))
                r = dynamicEffects[name];
            foreach (var technique in mainEffect.Techniques) {
                if (technique.Name.ToLower().StartsWith(name.ToLower()))
                    r = new FullTechnique(mainEffect, technique, technique.Name);
            }
            if (r == null) r = DynamicTechnique(name);
            if (r == null) throw new Exception("Technique not found for " + name);
            r = new FullTechnique(r);  // clone so we don't corrupt others
            r.parms = parms;
            return new FullTechnique(r);
        }

        /// <summary>
        /// generate a dynamic technique for the given name (TODO: make more generic)
        /// name may be xxx_N_M for an NxM filter, and also xxx_N for an NxN filter
        /// Unsharp_ 1..5
        /// Percentile_ 0..2  (requires a single percentile parameter)
        /// Median_ 0..2
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private FullTechnique DynamicTechnique(String name) {
            name = name.ToLower();
            string[] nn = name.Split('_');
            int pp0 = nn.Count() > 1 ? nn[1].Int() : 0;
            int pp1 = nn.Count() > 2 ? nn[2].Int() : pp0;
            FullTechnique ft = null;

            try {
                if (nn[0] == "median") ft = MedianTest(pp0, pp1);
                else if (nn[0] == "percentile") ft = PercentileTest(pp0, pp1);
                else if (nn[0] == "unsharp") ft = CreateEffectDirect(name, DynamicFx.Unsharp, "X", pp0 + "", "Y", pp1 + "");
                else return null;
            } catch (Exception e) {
                Form1.Beep("Cannot create technique " + name + "   " + e);
                return null;
            }

            string nname = nn[0] + "_" + pp0 + "_" + pp1;
            Console.Out.WriteLine("Generated technique " + nname);
            dynamicEffects[nname] = ft;
            if (pp0 == pp1) dynamicEffects[nn[0] + "_" + pp0] = ft;
            return ft;
        }



        /// <summary>image processing render pass on entire image; given input image, technique name, and parms</summary>
        public ImageTex Xrender(ImageTex inimage, String name, float[] parms) {
            FullTechnique technique = GetTechnique(name, parms);
            ImageTex nimage = inimage;
            nimage = Xrender(nimage, technique);
            return nimage;
        }

        /// <summary>image processing render pass on entire image; given input image, technique name</summary>
        public ImageTex Xrender(ImageTex inimage, String name) {
            return Xrender(new ImageTex[] { inimage }, name, null);
        }

        /// <summary>image processing render pass on entire image; given multiple input images, technique name</summary>
        public ImageTex Xrender(ImageTex[] inimages, String name) {
            return Xrender(inimages, name, null);
        }

        /// <summary>image processing render pass on entire image; given multiple input images, technique name, and parms</summary>
        public ImageTex Xrender(ImageTex[] inimages, String name, float[] parms) {
            FullTechnique technique = GetTechnique(name, parms);
            ImageTex nimage = Xrender(inimages, technique);
            return nimage;
        }

        /// <summary>image processing render pass on entire image; given input image and technique</summary>
        public ImageTex Xrender(ImageTex inimage, FullTechnique ltechnique) {
            return Xrender(new ImageTex[] { inimage }, ltechnique);
        }

        /// <summary>image processing render pass on entire image; given multiple input images and technique</summary>
        public ImageTex Xrender(ImageTex[] inimage, FullTechnique ltechnique) {
            return Xrender(inimage, ltechnique, Matrix.Identity,0,0);
        }

        // image processing render pass on entire image
        /// <summary>image processing render pass on entire image; given multiple input images, technique name, parameters, and transformation matrix</summary>
        public ImageTex Xrender(ImageTex[] inimage, string techniqueName, float[] parms, Matrix tran) {
            return Xrender(inimage, GetTechnique(techniqueName, parms), tran,0,0);
        }

        /// <summary>image processing render pass on entire image; 
        /// given multiple input images, technique name, parameters, transformation matrix and output size.
        /// The input images are in ImageTex form, wrapping textures already known to the device.  
        /// They are passed to the effect as parameters image, image1, image2 and image3.
        /// The result is captured back by getTexture(), and wrapped into an ImageTex.
        /// </summary>
        public ImageTex Xrender(ImageTex[] inimage, string techniqueName, float[] parms, Matrix tran, int ssizeX, int ssizeY) {
            return Xrender(inimage, GetTechnique(techniqueName, parms), tran, ssizeX, ssizeY);
        }

        static int SIZE = 2;
        RenderTarget2D[] rts = new RenderTarget2D[SIZE];
        int rtn = 0;

        /// <summary>image processing render pass on entire image; 
        /// given multiple input images, technique and transformation matrix and required output size
        /// </summary>
        internal ImageTex Xrender(ImageTex[] inimage, FullTechnique ftechnique, Matrix tran, int ssizeX, int ssizeY) {
            //*//if (dotime) timer.Start();
            EffectTechnique ltechnique = ftechnique.technique;
            Effect effect = ftechnique.effect;

            RenderTarget2D xxtarget;
            NextOp op = inimage[0].nextOpNew;  // STD, ALT, SHARE, NEW
            int isizeX = inimage[0].Width;
            int isizeY = inimage[0].Height;
            if (ssizeX == 0) ssizeX = isizeX;
            if (ssizeY == 0) ssizeY = isizeY;

            switch (op) {
                case NextOp.STD: xxtarget = xtarget; break;
                case NextOp.ALT: xxtarget = xtarget1; break;
                case NextOp.SHARE: rtn++; xxtarget = rts[rtn % SIZE]; break;
                case NextOp.NEW: xxtarget = null; break;
                default: throw new Exception("Unexpected NewOp " + op);
            }
            SurfaceFormat sf = useVector4 ? SurfaceFormat.Vector4 : graphicsDevice.DisplayMode.Format;
            if (xxtarget == null || xxtarget.Width != ssizeX || xxtarget.Height != ssizeY || xxtarget.Format != sf) {
                // programming note:  TODO: 
                // If the xxtarget is not the right size, I can't get things to work.
                // Even if it is too big and I cut it down with viewports.
                // This reallocation does not seem to matter too much with graphicsDevice.DisplayMode.Format, 
                // but is a performance problem with SurfaceFormat.Vector4.
                // We probably don't need Vector4 too much anyway, but if we do we should cache a few RenderTargets for the case of size changing often
                // (this multisize case arises a lot in the scanline test code).
                // Console.WriteLine("xxsize " + ssizeX + " " + ssizeY);
                xxtarget = new RenderTarget2D(graphicsDevice, ssizeX, ssizeY, 1, sf); 
                switch (op) {
                    case NextOp.STD: xtarget = xxtarget; break;
                    case NextOp.ALT: xtarget1 = xxtarget; break;
                    case NextOp.SHARE: rts[rtn % SIZE] = xxtarget; break;
                    case NextOp.NEW: break;
                    default: throw new Exception("Unexpected NewOp " + op);
                }
            }
        
            Viewport savevp = graphicsDevice.Viewport;    // this should match the screen size
            graphicsDevice.SetRenderTarget(0, xxtarget);  // << note that this changes graphicsDevice.Viewport to xxtarget = image size

            graphicsDevice.Clear(Microsoft.Xna.Framework.Graphics.Color.Gray); // this processing should often cover entire image, so no need to clear


            float h = 0.5f;
            //Matrix mat = new Matrix(h, 0, 0, 0, 0, -h, 0, 0, 0, 0, 1, 0, h + h / isizeX, h + h / isizeY, 0, 1);
            //Matrix ss0 = new Matrix(1, 0, 0, 0, 0, isizeY * 1.0f / isizeX, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1); 
            //Matrix ss1 = new Matrix(1, 0, 0, 0, 0, isizeX * 1.0f / isizeY, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            //mat = mat * ss0 * tran * ss1; // *mat;
            Matrix mat = new Matrix(h, 0, 0, 0, 0, -h, 0, 0, 0, 0, 1, 0, h + h / isizeX, h + h / isizeY, 0, 1);
            mat = mat * tran; // *mat;
            effect.Parameters["TexProj"].SetValue(mat);
            effect.Parameters["isize"].SetValue(new Vector2(isizeX, isizeY)); 
            effect.Parameters["stepImageXy"].SetValue(inimage[0].stepImageXy);
            ftechnique.SetParameters();
            if (inimage.Length >= 1) effect.Parameters["image"].SetValue(inimage[0].texture);
            if (inimage.Length >= 2) effect.Parameters["image2"].SetValue(inimage[1].texture);
            if (inimage.Length >= 3) effect.Parameters["image3"].SetValue(inimage[2].texture);
            if (inimage.Length >= 4) effect.Parameters["image4"].SetValue(inimage[3].texture);

            RunEffect(effect);
            graphicsDevice.SetRenderTarget(0, null);  // << note that this changes graphicsDevice.Viewport to (large) backBuffer size
            Texture2D result = xxtarget.GetTexture();
            graphicsDevice.Viewport = savevp;         // restore the back buffer

            // ? timer not reliable, work not complete behind the scenes
            //*//if (dotime) timer.time(ltechnique.Name);
            // TODO: improve fid text
            ImageTex r = new ImageTex(inimage[0].fid + ";" + ltechnique, result, this, inimage[0].fileInfo + " " + ftechnique.Name);
            // r.Show();
            return r;
        }


        /// <summary>load image from disc, set texture and isize as side effect</summary> 
        internal ImageTex LoadImage(String fileName) {
            ImageTex fileImage = new ImageTex(fileName, this);
            return fileImage;
        }

        /// <summary>run the effect.  This allows for multiple passes, though we only use single pass techniques.</summary> 
        internal void RunEffect(Effect effect) {
            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
                pass.Begin();
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, k * k * 2);
                pass.End();
            }
            effect.End();
        }


        /// <summary></summary> 
        public static readonly VertexElement[] VertexElements = new VertexElement[]
			{
			   new VertexElement(0, 0, VertexElementFormat.Vector3,
				VertexElementMethod.Default, VertexElementUsage.Position, 0),
            };


        /// <summary>subdivisions in each direction</summary> 
        int k = 16;  
        /// <summary>
        /// Set up a set of triangles that will be used to render the image
        /// This will use k*k squares.  k=1 should work for most things, 
        /// but larger may give more parallelism depending how graphics pipeline works.
        /// </summary> 
        internal void SetVertices() {
            // prepare a pair of triangles to fill -1..1 square
            float dd = 2f / k;
            Vector3[] mpos = new Vector3[6 * k * k];
            int p = 0;
            for (int i = 0; i < k; i++)
                for (int j = 0; j < k; j++) {
                    float x = 2 * dd * i - 1;
                    float y = 2 * dd * j - 1;
                    mpos[p++] = new Vector3(x - dd, y - dd, 0);
                    mpos[p++] = new Vector3(x - dd, y + dd, 0);
                    mpos[p++] = new Vector3(x + dd, y - dd, 0);

                    mpos[p++] = new Vector3(x - dd, y + dd, 0);
                    mpos[p++] = new Vector3(x + dd, y + dd, 0);
                    mpos[p++] = new Vector3(x + dd, y - dd, 0);
                }

            // and send to graphics
            int bufferFormatElementSize = 12; //  sizeof(Vector3); //  VertexPosition.SizeInBytes;
            VertexBuffer vertexBuffer = new VertexBuffer(graphicsDevice, bufferFormatElementSize * mpos.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData<Vector3>(mpos);

            graphicsDevice.VertexDeclaration = new VertexDeclaration(graphicsDevice, VertexElements);
            graphicsDevice.Vertices[0].SetSource(vertexBuffer, 0, bufferFormatElementSize);
        }
    }


    /// <summary>class to hold full information about a technique, which effect and which technique, and even which parms</summary> 
    public class FullTechnique {
        /// <summary>the technique (within effect)</summary> 
        internal EffectTechnique technique;
        /// <summary>the effect that holds the technique</summary> 
        internal Effect effect;
        /// <summary>the stepping used to display with this technique (used in convolutions??? TODO)</summary> 
        internal Vector2 stepKernelXy = new Vector2(1, 1);
        /// <summary>image parameter for the technqiue (used in convolutions)</summary> 
        internal Texture2D imageparm = null;
        /// <summary>other unnamed parameters used for the technique</summary> 
        internal float[] parms;
        /// <summary>construct a FullTechnique from and effect and technique</summary> 
        internal FullTechnique(Effect effect, EffectTechnique technique, string Name) {
            this.technique = technique;
            this.effect = effect;
            this.Name = Name;
        }
        /// <summary>name for the technique</summary> 
        public string Name;
        /// <summary>copy constructor for technique (eg use this if you want different parms)</summary> 
        internal FullTechnique(FullTechnique t) {
            technique = t.technique;
            effect = t.effect;
            stepKernelXy = t.stepKernelXy;
            imageparm = t.imageparm;
            parms = t.parms == null ? null : (float[]) (t.parms.Clone());
            Name = t.Name;
        }

        /// <summary>set the standard parameters: note that parms is a generic list with many different uses by different effects</summary> 
        public void SetParameters() {
            effect.Parameters["stepKernelXy"].SetValue(stepKernelXy);
            effect.Parameters["imageparm"].SetValue(imageparm);
            effect.Parameters["parms"].SetValue(parms);
            effect.CurrentTechnique = technique;
        }

    }




#if false  // statically loaded content

         //// load precompiled FXO  ~~~ can not get to work
        private void LoadContentFXO() {

            string cd = Directory.GetCurrentDirectory();
            string where = cd.Split(new string[] { @"\src\" }, StringSplitOptions.None)[0];
            string contentSource = where + @"\src\Image\Content\";
            try {
                string errstring = "";
                byte[] code = File.ReadAllBytes(contentSource + "image.fxo");
                int xtra = 4 - code.Length % 4;
                code = code.Concat(new byte[xtra]).Skip(4).ToArray();
                //CompiledEffect compiledEffect = new CompiledEffect(code, errstring);

                //if (compiledEffect.Success) {
                EffectPool effectPool = new EffectPool();
                effect = new Effect(graphicsDevice, code, CompilerOptions.None, effectPool);
                //}
                //else {
                //    System.Windows.Forms.MessageBox.Show("Error in shader '" + contentSource + "'.  Previous version will be used.\n\n" + compiledEffect.ErrorsAndWarnings);
                //}
            }
            catch (Exception e) {
                System.Windows.Forms.MessageBox.Show("Unexpected error in shader '" + contentSource + "'.  Previous version will be used.\n\n" + e);
            }
        }

        // load precompiled
        private void LoadContent() {
            // LoadContentRuntime(); return;
            string name = "image";
            GfxService gfxService = new GfxService(graphicsDevice);
            ServiceContainer services = new ServiceContainer();
            services.AddService<IGraphicsDeviceService>(gfxService);

            string cd = Directory.GetCurrentDirectory();
            string where = cd.Split(new string[] { @"\src\" }, StringSplitOptions.None)[0];
            string contentSource = where + @"\src\Image\Content\" + fxfile;
            string StartDirectory = @"D:\QuickHouse\Dev\iteration7\src\Image\bin\x86\Debug\Content\";
            ContentManager contentManager = new ContentManager(services, StartDirectory);
            try {
                effect = contentManager.Load<Effect>(name);
            }
            catch (Exception e) {
                System.Windows.Forms.MessageBox.Show("Cannot load compiled effect '" + name + "'.\n\n" + e);
            }
        }



    /// <summary>
    /// GfxService implements IGraphicsDeviceService to allow content loading.
    /// Originally just for fonts, now also used for shaders. (29 Oct 2009)
    /// </summary>
    class GfxService : IGraphicsDeviceService {
        GraphicsDevice gfxDevice;

        public GfxService(GraphicsDevice gfxDevice) {
            this.gfxDevice = gfxDevice;
            DeviceCreated = new EventHandler(DoNothing);
            DeviceDisposing = new EventHandler(DoNothing);
            DeviceReset = new EventHandler(DoNothing);
            DeviceResetting = new EventHandler(DoNothing);
        }

        public GraphicsDevice GraphicsDevice { get { return gfxDevice; } }

        public event EventHandler DeviceCreated;
        public event EventHandler DeviceDisposing;
        public event EventHandler DeviceReset;
        public event EventHandler DeviceResetting;

        void DoNothing(object o, EventArgs args) {
        }
    }


    /// <summary>
    /// Container class implements the IServiceProvider interface. This is used
    /// to pass shared services between different components, for instance the
    /// ContentManager uses it to locate the IGraphicsDeviceService implementation.
    /// </summary>
    public class ServiceContainer : IServiceProvider {
        Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// Adds a new service to the collection.
        /// </summary>
        public void AddService<T>(T service) {
            services.Add(typeof(T), service);
        }

        /// <summary>
        /// Looks up the specified service.
        /// </summary>
        public object GetService(Type serviceType) {
            object service;
            services.TryGetValue(serviceType, out service);
            return service;
        }
    }
#endif

}


