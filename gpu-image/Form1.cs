/* ---
 * lots on GUI
 * lots on features
 * zoom about cursor
 * lots on cleanup 
 * sort out optimization and looping for filters, and how compiled
 *   (current non-optimized compile seems to work reasonaly well, but ...)
 * more complete display rotate  
 * DONE: keys to loop through directory
 * DONE: do not recompute filter when only display has changed
 * DONE: pan speed relative to scale
 * DONE: easy display between processed/priginal (?add button as well)
 * DONE: do not redo compute part for redisplay
 * ---- */
using System;
using System.Collections.Generic;
using System.ComponentModel;
// using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using System.Reflection;

namespace Images {
    public partial class Form1 : Form {

        /// <summary>graphics device use for display (used just for creation, once created held and managed by Effects</summary>
        private GraphicsDevice GraphicsDevice { get { return Effects.graphicsDevice; } }

        /// <summary>the presentation information, used when the XNAControlSet/GraphicsDevice/RenderFramework is set up, and for recovery</summary>
        public PresentationParameters pres = null;

        /// <summary>antialias size</summary> 
        int anti = 1; // antialias size

        /// <summary>Effects object used to control all the effects, and general handle to graphicsDevice etc</summary>
        internal Effects Effects;

        /// <summary>the technique that will be used in the final display to screen phase of display</summary>
        private FullTechnique displayTechnique;

        /// <summary>last clicked technique button (used to highlight)</summary> 
        ToolStripButton techniqueButton = null;

        /// <summary>show a new technique button as 'active'</summary> 
        private void showtechnique(object s) {
            ToolStripButton b = (ToolStripButton)s;
            if (techniqueButton != null) techniqueButton.ForeColor = System.Drawing.Color.Black;
            techniqueButton = b;
            b.ForeColor = System.Drawing.Color.Red;
        }

        /// <summary>set the current technique, update the buttons and the image</summary> 
        private void settechnique(object s, string t) {
            displayTechnique = Effects.GetTechnique(t, null);
            showtechnique(s);
            RunProgram();
        }

        /// <summary>file name for initial test image</summary> 
        private string fileName = @"Content\test.jpg";


        /// <summary>current loaded image as an ImageTex</summary> 
        private ImageTex fileImage;
        /// <summary>current processed image as an ImageTex</summary> 
        private ImageTex processedImage;

        /// <summary>pan values in current use</summary> 
        private Vector2 pan = new Vector2();    // pan position (screen)
        /// <summary>scale value in current use</summary> 
        private float scale = 1;                // scale, 1 fits screen
        /// <summary>rotate flag (TODO: more orientations needed)</summary> 
        private bool rotate = false;

        /// <summary>if true, show original, not processed image</summary> 
        private bool showOriginal; 

        /// <summary>the internal pixel scale</summary> 
        private float _pixscale;  // pixel scale

        /// <summary>current form, so we can have a singleton</summary> 
        private static Form1 current;
        /// <summary>current form, so we can have a singleton (used by some Image convenience functions)</summary> 
        public static Form1 _ { get { return current; } }
        /// <summary>Show image in current (default) form</summary>
        public static void ShowImageDefault(ImageTex im) { current.ShowImage(im); }
        /// <summary>Show small image in current (default) form (timing and debug)</summary>
        public static void ShowImageDefaultSmall(ImageTex im) { current.ShowImage(im, true); }

        /// <summary>make a button with given text and eventHandler, and place on top bar</summary> 
        private ToolStripButton Button(string text, EventHandler eh, string tip) {
            ToolStripButton b = new ToolStripButton(text);
            b.Click += eh;
            toolStrip1.Items.Add(b);
            b.ToolTipText = tip;
            return b;
        }

        /// <summary>make a label and place on top bar</summary> 
        private void Label(string text) {
            ToolStripLabel l = new ToolStripLabel(text);
            toolStrip1.Items.Add(l);
        }

        /// <summary>make a textbox with given text associated with given fieldname, and place on top bar</summary> 
        private void TextBox(string text, string fieldname, string tip) { // EventHandler eh) {
            FieldInfo f = GetType().GetField(fieldname);
            if (f == null) { MessageBox.Show("No field found for " + fieldname); return; } 
            ToolStripTextBoxX b = new ToolStripTextBoxX(text, f, this);
            toolStrip1.Items.Add(b);
            b.ToolTipText = tip;
        }

        /// <summary>add a separator to the top bar</summary> 
        private void Sep() {
            toolStrip1.Items.Add(new ToolStripSeparator());
        }

        /// <summary>if true, update image for every keystroke rather than waiting for ctrl or doubleclick</summary> 
        private CheckBox continuous = new CheckBox();

        /// <summary>if true, use Vector4 for renderTargets</summary> 
        private CheckBox useVector4 = new CheckBox();

        ToolTip toolTip = new ToolTip();  // just one, used for several controls

        /// <summary>make a new Form1</summary> 
        public Form1(string[] args) {
            InitializeComponent();
            //Convolutions = new Convolutions(this);

            string cd = Utils.GetCommandLineA(); //  Directory.GetCurrentDirectory();
            if (cd.Contains('\"')) {
                cd = cd.Split('\"')[1];
                string where = cd.Split(new string[] { @"\gpu-image\" }, StringSplitOptions.None)[0];
                Directory.SetCurrentDirectory(where + @"\gpu-image\");
            }

            if (args.Length > 0) fileName = args[0];

            programBox.Text = "# write program here;\r\n# ctrl key or double click to execute";

            continuous.Text = "continuous";
            toolTip.SetToolTip(continuous, "enable to execute program every keystroke, otherwise use dblClick or ctrl key");
            ToolStripControlHost tsch = new ToolStripControlHost(continuous);
            toolStrip1.Items.Insert(0, tsch);

            useVector4.Text = "useVector4";
            toolTip.SetToolTip(continuous, "use Vector4 for intermediate render targets");
            ToolStripControlHost tsch2 = new ToolStripControlHost(useVector4);
            toolStrip1.Items.Insert(0, tsch2);
            useVector4.CheckedChanged += (s, e) => { Effects.useVector4 = useVector4.Checked; RunProgram(); };

            Button("SaveProg", saveProg_Click, "Save the image processing program");
            Button("LoadProg", loadProg_Click, "Load an image processing program");
            Button("RerunProg", (s, e) => RunProgram(), "Rerun the image processing program");
            Button("TimeProg", time_Click, "Time the image processing program");
            Sep();

            Button("LoadImage", openImage_Click, "Load an new current image");
            Button("SaveImage", saveImage_Click, "Save current processed image");
            Button("ReloadEffects", (s, e) => { Effects.LoadContentRuntime(); RunProgram(); }, "Reload all the effects");
            Sep();

            Button("Rotate", (s, e) => { rotate = !rotate; RunProgram(); }, "Rotate the image");
            Button("Reset", reset_Click, "Reset");

            Button("Median", (s, e) => { settechnique(s, "Median"); }, "Use median filter for the display output stage");
            Button("Direct", (s, e) => { settechnique(s, "Direct"); }, "Use direct (noop) filter for the display output stage");
            Button("Bilinear", (s, e) => { settechnique(s, "Bilinear"); }, "Use bilinear filter for the display output stage");
            ToolStripButton lanc = Button("Lanc", (s, e) => { showtechnique(s); SetLanc(); }, "Use Lanczos filter for the display output stage");
            ;
            Label("a"); TextBox("2", "LancA", "number of harmonics for Lanczos filter (A=2)");
            Label("w"); TextBox("2", "LancW", "width of Lanczos filter (W=2)");
            Label("sqw"); TextBox("1", "SQWidth", "width of square filter convolved with Lanczos filter (SQWidth=1)");
            Label("circp"); TextBox("2", "CircP", "circle/superEgg power for shape of filter (CircP=2)");
            //''Button("OldBilin", (s, e) => { settechnique(s, "OldBilinear"); ShowImage(); }, "Use old version of bilinear filter for the display output stage");
            Button("ShowOriginal", (s, e) => { showOriginal = !showOriginal; ShowImage(); }, "Show original non-processed image");

            Sep();
            Button("Draw Filter", (s, e) => { SetDraw(); }, "Draw the display convolution filter");
            Button("RunInterpreter", (e, s) => RunInterpreter(), "Open an interpreter window for C# programs");
            Button("Test", (s, e) => Test(), "Run test");

            // copy these events for fulls creen as well
            imageSurrogate.MouseWheel += MouseWheelCode;
            programBox.MouseWheel += MouseWheelCode; // why is this stealing mousewheel ~ anyway, this works for now.
            // MouseEventHandler.RegisterClassHandler(typeof(Form1), null /* System.Windows.RoutedEvent routedEvent*/, MouseWheelCode, true);

            image.MouseMove += MouseMoveCode;
            image.MouseDown += MouseDownCode;
            image.Click += (s, e) => imageSurrogate.Focus();
            image.Paint += (s, e) => ShowImage("image.paint");
            // image.Resize += (s, e) => ShowImage("image.resize");
            //image.Invalidated += (s, e) => ShowImage("image.Invalidated");
            //this.GotFocus += (s, e) => ShowImage("this.GotFocus");
            //this.Activated += (s, e) => ShowImage("this.Activated");
            imageSurrogate.SendToBack();

            programBox.MouseDown += TMouseDownCode;
            programBox.MouseMove += TMouseMoveCode;
            programBox.MouseMove += (s, e) => { if (e.Button == MouseButtons.Right) RunProgram(); };

            programBox.ContextMenu = new ContextMenu();

            showtechnique(lanc);

            AllowDrop = true;
            DragDrop += DragDropCode;
            DragEnter += DragEnterCode;

            InitializeRenderFramework(image);
            // SetLanc();  // now part of InitializeRenderFramework
            LoadImage();
            Width = Screen.PrimaryScreen.WorkingArea.Width;  // *2 / 3;
            Form1_Resize();  // force initial layout of image etc

            current = this;

            // RunCsi.XMain(new string[] {});
        }

        /// <summary>load image from disc, remember as main input image, and display</summary> 
        private void LoadImage() {
            fileImage = Effects.LoadImage(fileName);
            RunProgram();
        }


        /// <summary>move to next image in current directory</summary> 
        private void NextImage(int d) {
            string dirname = fileName.Substring(0, fileName.LastIndexOf("\\"));
            string[] dirfiles = Directory.GetFiles(dirname, "*.jpg");
            int L = dirfiles.Length;
            for (int i = 0; i < L; i++) {
                if (dirfiles[i] == fileName) {
                    fileName = dirfiles[(i + L + d) % L];
                    LoadImage();
                    return;
                }
            }
        }

        /// <summary>write an exception to stdout and beep</summary> 
        public static void Beep(Exception e) { Console.WriteLine("beep: " + e); Console.Beep(); }
        /// <summary>write a string to stdout and beep</summary> 
        public static void Beep(string s) { Console.WriteLine("beep: " + s); Console.Beep(); }

        Viewport xvp; // extra viewport for test

        private ImageTex RunProgram() {
            try {
                return RunProgramX();
            } catch (DeviceLostException) {
                Console.WriteLine("attempt to recover after deviceLost during RunProgram()");
                RecoverDevice();
                return null;
            }
        }

        /// <summary>
        /// very basic parsing and execution of a program
        /// comma separated 'lines'
        /// each line contains '[repeat] name [parms]*'
        ///     eg '6 med', 'trap 0.1 0 0 0', 'Percentile2 0.7'
        /// special values of parms are 'sharp' and 'smooth' (for use with Avg3)
        /// special values of name are 'trap' and 'lanc' to control display phase, and 'trapx' for compute pipeline distort 
        /// ! as a start line character at any point cuts execution at that point:     eg a,!b,c === a
        /// # as a start line character at any point prevents execution of that line@  eg a,#b,c === a,c
        /// </summary>
        private ImageTex RunProgramX() {
            trapm = Matrix.Identity;  // unless otherwise set
            Viewport savevp;
            savevp = xvp = GraphicsDevice.Viewport;  // unless otherwise set
            GraphicsDevice.SetRenderTarget(0, null);
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Graphics.Color.Gray);
            Boolean autoshow = true;  // show at end unless explicitly handled
            int outputSizeX = 0, outputSizeY = 0;  // output size if not default

            ImageTex nimage = fileImage;
            // String[] lines = Program.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            programBox.ForeColor = System.Drawing.Color.Orange;
            string[] lines = programBox.Lines;
            int cline = programBox.GetLineFromCharIndex(programBox.SelectionStart);
            if (lines.Count() == 0) return nimage;
            if (lines[cline].Length > 0 && lines[cline][0] == '!') { Beep("! line selected"); return nimage; }
            while (cline != 0 && (lines[cline-1].Length == 0 || lines[cline-1][0] != '!')) cline--;  // count up to start of section

            string xline = "";  // to allow for continuations
            for (int il=cline; il<lines.Length; il++) {
                string line = (xline + lines[il]).Trim();
                if (line.EndsWith("_")) { xline = line.Remove(line.Length - 1) + " "; continue; }
                xline = "";
                String[] sparms = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (sparms.Length > 0) {
                    string name = sparms[0];
                    if (name[0] == '!') break;
                    if (name[0] == '#') continue;
                    if (name == "ref") { Effects.LoadContentRuntime(); continue; }
                    if (name == "<") name = "lt";
                    //if (line.Contains('=')) {
                    //    String[] cparms = line.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    //    Effects.CreateEffectDirect(cparms[0], cparms[1]);
                    //    continue;
                    //}
                    int repeat = 1;
                    if ('0' <= name[0] && name[0] <= '9') {
                        repeat = name.Int();
                        sparms = sparms.ToList().GetRange(1, sparms.Length - 1).ToArray();
                        name = sparms.Length == 0 || sparms[0] == "" ? "NONE" : sparms[0];
                    }

                    float[] parms = new float[100];
                    if (sparms.Length > 1) switch (sparms[1]) {
                            // NOTE: parameter 0 gives the number of parameters, not generally sed but always expected
                            case "sharp": parms = new float[] { 9, 0, -1, 0, -1, 5, -1, 0, -1, 0 }; break;
                            case "smooth": parms = new float[] { 9, 1f / 9, 1f / 9, 1f / 9, 1f / 9, 1f / 9, 1f / 9, 1f / 9, 1f / 9, 1f / 9 }; break;
                            case "unsharp": parms = new float[] { 11, 1, 0.1f, 1, 0.6f, 0.36f, 0.2f, 0.1f, 0, 0, 0, 0 }; break;
                            default:
                                parms[0] = sparms.Length;
                                for (int i = 1; i < sparms.Length; i++) {
                                    if (sparms[i] == "...")
                                        for (; i < parms.Length; i++) parms[i] = parms[i - 1];
                                    else
                                        parms[i] = sparms[i].Float();
                                }
                                break;
                        }

                    ImageTex[] images = new ImageTex[] { nimage };  // input images to next stage, unless overridden

                    if (name == "trap") {  // distort using output matrix
                        trapm = Matrix.Identity;
                        trapm.M12 = parms[1];
                        trapm.M14 = parms[2];
                        trapm.M21 = parms[3];
                        trapm.M24 = parms[4];
                        continue;
                    }
                    if (name == "vp") {  // set explicit viewport and display this step in it
                        xvp = new Viewport();
                        xvp.X = (int)parms[1];
                        xvp.Y = (int)parms[2];
                        xvp.Width = (int)parms[3];// +xvp.X;
                        xvp.Height = (int)parms[4];// +xvp.Y;
                        ShowImageNoClear(nimage, false);
                        autoshow = false;
                        continue;
                    }
                    if (name == "vpr") {  // set explicit viewport and display this step in it
                        Viewport o = GraphicsDevice.Viewport;
                        xvp = new Viewport();
                        xvp.X = (int)(parms[1]*o.Width);
                        xvp.Y = (int)(parms[2]*o.Height);
                        xvp.Width = (int)(parms[3] * o.Width);// +xvp.X;
                        xvp.Height = (int)(parms[4] * o.Height);// +xvp.Y;
                        ShowImageNoClear(nimage, false);
                        autoshow = false;
                        continue;
                    }
                    if (name == "clear") {
                        GraphicsDevice.Clear(new Microsoft.Xna.Framework.Graphics.Color(parms[1], parms[2], parms[3], parms[4]));
                        continue;
                    }

                    if (name == "new") {  // use new output
                        nimage = Effects.Xrender(nimage, "bilin");
                        nimage.nextOpNew = NextOp.NEW;
                        continue;
                    }
                    if (name == "alt") {  // use alt output
                        nimage = Effects.Xrender(nimage, "bilin");
                        nimage.nextOpNew = NextOp.ALT;
                        continue;
                    }
                    if (name == "size") {  // control output size of the next filter in the pipeline
                        outputSizeX = (int)parms[1];
                        outputSizeY = (int)parms[2];
                        continue;
                    }
                    if (name == "rsize") {  // control relative output size of the next filter in the pipeline
                        outputSizeX = (int)(parms[1] * nimage.Width);
                        if (outputSizeX == 0 && parms[1] != 0) outputSizeX = 1;
                        outputSizeY = (int)(parms[2] * nimage.Height);
                        if (outputSizeY == 0 && parms[2] != 0) outputSizeY = 1;
                        continue;
                    }
                    if (name == "irsize") {  // control relative output size using inverse of the next filter in the pipeline
                        outputSizeX = parms[1] == 0 ? 0 : (int)(nimage.Width / parms[1]);
                        if (outputSizeX == 0 && parms[1] != 0) outputSizeX = 1;
                        outputSizeY = parms[2] == 0 ? 0 : (int)(nimage.Height / parms[2]);
                        if (outputSizeY == 0 && parms[2] != 0) outputSizeY = 1;
                        continue;
                    }
                    if (name == "trapx") {  // trapezoid distort using output matrix
                        Matrix trapmm = Matrix.Identity;
                        trapmm.M12 = parms[1];
                        trapmm.M14 = parms[2];
                        trapmm.M21 = parms[3];
                        trapmm.M24 = parms[4];
                        nimage = Effects.Xrender(images, displayTechnique, trapmm, (int)parms[5], (int)parms[6]);
                        continue;
                    }
                    if (name == "lanc") { // set up parameters for lanc
                        if (parms.Length > 1) LancA = (int)parms[1];
                        if (parms.Length > 2) LancW = parms[2];
                        if (parms.Length > 3) SQWidth = parms[3];
                        if (parms.Length > 4) CircP = parms[4];
                        ToolStripTextBoxX.RefreshAll();
                        SetLanc();
                        continue;
                    }
                    if (name.EndsWith("map")) { // setup for various maps
                        ToneCurve toneCurve = new ToneCurve(Effects, name, parms); 
                        name = "tonemap";
                        images = new ImageTex[] { nimage, toneCurve };
                        if (drawer != null) {
                            drawer.DrawClear();
                            drawer.Draw(toneCurve.toneCurve);
                        }
                    }


                    try {
                        if (name != "NONE")
                            for (int i = 0; i < repeat; i++)
                                nimage = Effects.Xrender(images, name, parms, Matrix.Identity, outputSizeX, outputSizeY);
                        outputSizeX = outputSizeY = 0;

                    } catch (Exception e) {
                        Console.WriteLine("Exception during RunProgram - " + e);
                        Beep(e);
                    }
                }
            }
            processedImage = nimage;
            showOriginal = false;
            programBox.ForeColor = System.Drawing.Color.Green;
            if (autoshow) ShowImageNoClear(nimage, false);
            GraphicsDevice.Viewport = savevp;
            return nimage;
        }

        /// <summary>show the current image with current parameters, and display debug string on console</summary>
        private void ShowImage(string s) {
            Console.Write(s + ":  ");
            ShowImage();
        }

        /// <summary>show current image with current parameters: either the original as loaded, or the processed image</summary>
        private void ShowImage() {
            ShowImage(showOriginal ? fileImage : processedImage);
        }

        /// <summary>show the given image with current parameters</summary>
        internal void ShowImage(ImageTex iimage) {
            ShowImage(iimage, false);
        }

        //// cheat places for extra parameters
        //public Matrix[] XTran = null;
        //public Image image2;
        //public float[] Xparms = null;
        //public string Xtechnique;

        //void ExtraParameters(Effect effect) {
        //    if (Xtechnique != null) {
        //        FullTechnique ft = GetTechnique(Xtechnique);
        //        effect = ft.effect;
        //        effect.CurrentTechnique = ft.technique;
        //        Xtechnique = null;
        //    }

        //    if (image2 != null) effect.Parameters["image2"].SetValue(image2.texture);
        //    Matrix basic = effect.Parameters["TexProj"].GetValueMatrix();
        //    if (XTran != null) {
        //        if (XTran.Length > 0) effect.Parameters["TexProj"].SetValue(XTran[0] * basic);
        //        if (XTran.Length > 1) effect.Parameters["TexProj2"].SetValue(XTran[1] * basic);
        //        if (XTran.Length > 2) effect.Parameters["TexProj3"].SetValue(XTran[2] * basic);
        //        if (XTran.Length > 3) effect.Parameters["TexProj4"].SetValue(XTran[3] * basic);
        //        XTran = null;
        //    }
        //    if (Xparms != null) {
        //        effect.Parameters["parms"].SetValue(Xparms);
        //        Xparms = null;
        //    }
        //}


        /// <summary>
        /// initialize the render framework for our use
        /// The control used as DeviceWindowHandle will just be the first control we happen to render to.
        /// Future rendering will ignore this as the Present(handle) call is used;
        /// but the other controls ought to look similar to the first control (not necessarily in size, but in colour depth etc)
        /// </summary>
        /// <param name="control"></param>
        private void InitializeRenderFramework(Control control) {
            if (control.IsDisposed) { BREAK(); }
            try {
                if (pres != null) return;
                pres = new PresentationParameters();
                pres.BackBufferCount = 1;
                pres.BackBufferFormat = SurfaceFormat.Unknown;
                pres.BackBufferHeight = 4; //  small to force resizeBackBuffer and its bits
                pres.BackBufferWidth = 4; //  1800;
                pres.DeviceWindowHandle = control.Handle;
                pres.IsFullScreen = false;
                pres.SwapEffect = SwapEffect.Discard;
                pres.AutoDepthStencilFormat = DepthFormat.Depth24;  // ???
                pres.EnableAutoDepthStencil = false;  // was true

                GraphicsDevice GraphicsDevice = new GraphicsDevice(
                    GraphicsAdapter.DefaultAdapter,
                    DeviceType.Hardware,
                    control.Handle,
                    pres);
                //Console.WriteLine("new GraphicsDevice " + GraphicsDevice.GetHashCode());

                Effects = new Effects(GraphicsDevice);
                SetLanc();  // at least make sure some displayTechnique is correct
                resizeBackBuffer(control);  // also set antialias etc
            } catch (Exception e) {
                MessageBox.Show("Cannot initialize RenderFramework ~ ? incompatible graphics hardware\n" +
                    "Will render in 2d\n" + e);
            }
        }

        /// <summary>small viewport for display of small image (timing and debug)</summary> 
        private Viewport SmallVP;

        // private RenderTarget2D screenTarget; // experiment to render direct to screen

        /// <summary>
        /// Resize the back buffer to match the control, or the capture array if present
        /// The backBuffer is allowed to grow, but not shrink.  Inefficient to change the size frequently.
        /// </summary>
        private void resizeBackBuffer(Control xcontrol) {
            if (GraphicsDevice == null) return;
            GraphicsDevice.RenderState.MultiSampleAntiAlias = (anti > 1);
            if (anti > 1) {
                GraphicsDevice.RenderState.MultiSampleAntiAlias = true;
                pres.MultiSampleType = (MultiSampleType)anti;
            } else {
                GraphicsDevice.RenderState.MultiSampleAntiAlias = false;
                pres.MultiSampleType = MultiSampleType.None;
            }

            GraphicsDevice.RenderState.CullMode = CullMode.None;
            GraphicsDevice.RenderState.FillMode = FillMode.Solid; // ?? not needed?

            int w = xcontrol.ClientSize.Width, h = xcontrol.ClientSize.Height;
            Console.WriteLine("width " + w + ", height " + h);

            //// to avoid over frequent resize of the back buffer, just let it grow
            //// we had bad performance with ganged transform windows of mixed size, sjtp 15 Jan 2009
            int mw = Math.Max(w, GraphicsDevice.PresentationParameters.BackBufferWidth);
            int mh = Math.Max(h, GraphicsDevice.PresentationParameters.BackBufferHeight);

            // w = 100; h = 100;

            if (mw > 16 && mh > 16 &&
                (mw != GraphicsDevice.PresentationParameters.BackBufferWidth ||
                    mh != GraphicsDevice.PresentationParameters.BackBufferHeight)) {
                // TraceN.trace(4, "device size reset {0} {1} -> {2} {3}", pres.BackBufferWidth, pres.BackBufferHeight, mw, mh);
                pres.BackBufferWidth = mw;
                pres.BackBufferHeight = mh;
                GraphicsDevice.Reset(pres);
                // doing some minimum render at this point forces the first Clear() to be interpreted with
                // correct sRBG etc correction ~ see note at end of this file
                // RenderFramework.current.RenderLightModel(Matrix.Identity);
            }

            Viewport vp = new Viewport(); vp.Height = h; vp.Width = w; vp.MinDepth = GraphicsDevice.Viewport.MinDepth; vp.MaxDepth = GraphicsDevice.Viewport.MaxDepth;
            xvp = GraphicsDevice.Viewport = vp;

            SmallVP = new Viewport();
            SmallVP.Height = 10; SmallVP.Width = 10; SmallVP.MinDepth = GraphicsDevice.Viewport.MinDepth; SmallVP.MaxDepth = GraphicsDevice.Viewport.MaxDepth;

            Effects.SetVertices();  // must be redone now things have changed

            RunProgram();
            //screenTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 1, GraphicsDevice.DisplayMode.Format);
        }

        /// <summary>previous startTime for timing</summary> 
        private long st = 0;
        /// <summary>end time interval and display time take, with string</summary> 
        private void time(string m) {
            Console.WriteLine(m + "  time = " + (Environment.TickCount - st));
            time();
        }
        /// <summary>start time interval</summary>
        private void time() {
            st = Environment.TickCount;
        }

        /// <summary> put code in here for testing</summary>
        private void Test() {
            time();
            byte[] timdata = processedImage.data;
            time("time to read data back");

            byte[, ,] timdatav = new byte[fileImage.Height, fileImage.Width, 4];
            int k = 0;
            for (int i = 0; i < fileImage.Height; i++)
                for (int j = 0; j < fileImage.Width; j++)
                    for (int c = 0; c < 4; c++)
                        timdatav[i, j, c] = timdata[k++];
            time("time to convert");

            //byte bbb = bb[456];
            //ByteBuffer q = ByteBuffer.wrap(bb);
            //byte[,,] barr = new byte[10,10,4];
            //q.get(barr, 0, 400);
            /**
 for (int i = 0; i <= 360; i+=10) {   Image r = (i1.Rot(i).s + i2.Rot(-i)).s; }         
             * **/

            time();
            /**
            Image i3;
            if (Image.i1 == null) Image.i1 = new ImageTex(@"D:\share\Pictures\2010\2010-01 Kitty Peru\DSCF3815.JPG");
            if (Image.i2 == null) Image.i2 = new ImageTex(@"D:\share\Pictures\2010\2010-01 Kitty Peru\DSCF3816.JPG");
            Image i1 = Image.i1;
            Image i2 = Image.i2;
            time("images loaded");
                        for (int i = 0; i <= 390; i+=10) { i3 = (i1.Rot(i) + i2.Rot(-i)); }
                        time("no display, ? wrong answer");
                        for (int i = 0; i <= 390; i += 10) { i3 = (i1.Rot(i) + i2.Rot(-i)).s; }
                        time("display final (? wrong answer)");
                        for (int i = 0; i <= 390; i += 10) { i3 = (i1.Rot(i).s + i2.Rot(-i)).s; }
                        time("display left and final (? wrong answer)");
                        for (int i = 0; i <= 390; i += 10) { i3 = (i1.Rot(i).s + i2.Rot(-i).s).s; }
                        time("display left, right and final (? wrong answer)");

                        for (int i = 0; i <= 390; i += 10) { i3 = (i1.q.Rot(i) + i2.Rot(-i)); }
                        time("no display, shared, ? right answer");
                        for (int i = 0; i <= 390; i += 10) { i3 = (i1.q.Rot(i) + i2.Rot(-i)).s; }
                        time("display final, shared , ? right answer");
                        for (int i = 0; i <= 390; i += 10) { i3 = (i1.q.Rot(i).s + i2.Rot(-i)).s; }
                        time("display left final, shared , ? right answer");
                        for (int i = 0; i <= 390; i += 10) { i3 = (i1.q.Rot(i).ss + i2.Rot(-i)).s; }
                        time("display leftsmall final, shared , ? right answer");
                        for (int i = 0; i <= 390; i += 10) { i3 = (i1.q.Rot(i).ss + i2.Rot(-i)).ss; }
                        time("display leftsmall finalsmall, shared , ? right answer");
            for (int i = 0; i <= 390; i += 10) { i3 = (i1.Rot(i) + i2.Rot(-i)).s; }
            time("display final, common, wrong answer");
            for (int i = 0; i <= 390; i += 10) { i3 = (i1.q.Rot(i) + i2.Rot(-i)).s; }
            time("display final, alt , ? right answer");
            //for (int i = 0; i <= 390; i += 10) { i3 = (i1.qq.Rot(i) + i2.Rot(-i)).s; }
            //time("display final, shared , ? right answer");
            //for (int i = 0; i <= 390; i += 10) { i3 = (i1.qqq.Rot(i) + i2.Rot(-i)).s; }
            //time("display final, new , ? right answer");

            for (int i = 0; i <= 390; i += 10) { i3 = (i1.q.Rot(i) + i2.Rot(-i)).data; }
            time("data final, alt , ? right answer");
            string s = "col = float4(1,0,0,1);";
            if (Effects.CreateEffectViaFile("test", s) != null) {

                for (int i = 0; i < 100; i++) Effects.CreateEffectViaFile("test", s);
                time("CreateEffectToFile");
                for (int i = 0; i < 100; i++) Effects.CreateEffectDirect("test", s);
                time("CreateEffectDirect");
            }
            **/

        }

        private void openImage_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            if (fd.ShowDialog() == DialogResult.OK) {
                fileName = fd.FileName;
                LoadImage();
            }
        }

        private void saveImage_Click(object sender, EventArgs e) {
            processedImage.Save();
        }


        private void MouseWheelCode(object sender, MouseEventArgs e) {
            // todo, scale about cursor
            float clicks = e.Delta / 100;
            scale *= (float)Math.Pow(2, -clicks * 0.25);
            //Console.WriteLine("delta={0} clicks={1} scale={2}", e.Delta, clicks, scale);
            RunProgram();
        }

        private void DragDropCode(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop)) {
                string[] o = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);
                fileName = o[0];
                LoadImage();
            }
        }
        private void DragEnterCode(object sender, DragEventArgs e) {
            e.Effect = DragDropEffects.Move;
        }

        private int lastX = 0, lastY = 0;
        private void MouseMoveCode(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                int dx = e.X - lastX, dy = e.Y - lastY;
                float rdx = dx * 0.5f / image.Width, rdy = dy * 0.5f / image.Height;
                pan.X -= 2 * scale * rdx;
                pan.Y -= 2 * scale * rdy;
                lastX = e.X; lastY = e.Y;
                ShowImage();
            }
        }

        private void MouseDownCode(object sender, MouseEventArgs e) {
            lastX = e.X; lastY = e.Y;
        }

        // reset toggles between reset and previous
        private bool init = true;
        private float savescale;
        private Vector2 savepan;
        private void FitToScreen() {
            if (init) {
                savescale = scale;
                savepan = pan;
                scale = 1;
                pan = new Vector2();
            } else {
                scale = savescale;
                pan = savepan;
            }
            init = !init;
            ShowImage();
        }

        private void reset_Click(object sender, EventArgs e) {
            FitToScreen();
        }


        private void programBox_TextChanged(object sender, EventArgs e) {
            if (continuous.Checked) {
                RunProgram();
            } else {
                programBox.ForeColor = System.Drawing.Color.Red;
            }
        }

        private void Form1_Resize(object sender, EventArgs e) {
            Form1_Resize();
        }
        private void Form1_Resize() {
            // docking does not work as I would want, image is hid behind programbox
            image.Left = programBox.Width;
            image.Top = toolStripContainer1.TopToolStripPanel.Height;
            image.Width = image.Parent.ClientSize.Width - image.Left;
            image.Height = image.Parent.ClientSize.Height - image.Top;
            programBox.Top = image.Top;
            programBox.Height = image.Height;

            resizeBackBuffer(image);
            //ShowImage();

            image.Invalidate();
        }

        private void RunInterpreter() {
            Interpreter interp = RunCsi.XMain(new string[] { });
            interp.AddReference(@"C:\Program Files\Microsoft XNA\XNA Game Studio\v3.1\References\Windows\x86\Microsoft.Xna.Framework.dll");
            interp.AddReference(@"C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll");
            interp.AddNamespace("Images");
            Interpreter.Console.Write("i1 = new ImageTex(@\"" + fileName + "\")");
        }

        private void loadProg_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.AddExtension = true;
            fd.Filter = "Image Filter Files|*.ifs";
            fd.DefaultExt = ".ifs";
            if (fd.ShowDialog() == DialogResult.OK) {
                StreamReader sr = new StreamReader(fd.FileName);
                programBox.Text = sr.ReadToEnd();
                sr.Close();
            }
            RunProgram();
        }

        private void saveProg_Click(object sender, EventArgs e) {
            SaveFileDialog fd = new SaveFileDialog();
            fd.AddExtension = true;
            fd.Filter = "Image Filter Files|*.ifs";
            fd.DefaultExt = ".ifs";
            if (fd.ShowDialog() == DialogResult.OK) {
                StreamWriter sw = new StreamWriter(fd.FileName);
                {
                    sw.Write(programBox.Text);
                    sw.Close();
                }
            }

        }

        private void programBox_KeyDown(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.ControlKey: // run the program at the current selection position
                    RunProgram(); 
                    break;
                case Keys.PageUp:  // move up to the previous program (above !) and run it
                    string[] lines = programBox.Lines;
                    int cline = programBox.GetLineFromCharIndex(programBox.SelectionStart);
                    if (lines.Count() == 0) return;
                    for (; cline > 0; cline--) {
                        if (lines[cline].Length > 0 && lines[cline][0] == '!') break;
                    }
                    cline--;
                    for (; cline > 0; cline--) {
                        if (lines[cline].Length > 0 && lines[cline][0] == '!') break;
                    }
                    cline++;
                    if (cline == 0) cline = lines.Count() - 1;
                    programBox.SelectionStart = programBox.GetFirstCharIndexFromLine(cline);
                    RunProgram(); 
                    break;
                case Keys.PageDown: // move down to the next program (below !) and run it
                    lines = programBox.Lines;
                    cline = programBox.GetLineFromCharIndex(programBox.SelectionStart);
                    if (lines.Count() == 0) return;
                    for (; cline < lines.Count()-1; cline++) {
                        if (lines[cline].Length > 0 && lines[cline][0] == '!') break;
                    }
                    cline++;
                    if (cline >= lines.Count()) cline = 0; //  lines.Count() - 1;
                    programBox.SelectionStart = programBox.GetFirstCharIndexFromLine(cline);
                    RunProgram();
                    break;

            }
        }

        private void programBox_Leave(object sender, EventArgs e) {
            RunProgram();
        }

        private void programBox_DoubleClick(object sender, EventArgs e) {
            RunProgram();
        }

        private void toolStripContainer1_TopToolStripPanel_Click(object sender, EventArgs e) {

        }

        private void time_Click(object sender, EventArgs e) {
            long st = Environment.TickCount;
            long dt = 0;
            int i;
            for (i = 0; i < 10000; i++) {
                RunProgram();
                dt = Environment.TickCount - st;
                if (dt > 2000) break;
            }
            String s = String.Format("time for {2}={0} millesec, each {1} millesec:  {3} ", dt, (int)(dt / (i + 1)), (i + 1), programBox.Text);
            MessageBox.Show(s);
            Console.WriteLine(s.Replace("\r\n", "; "));

        }

        private void imageSurrogate_KeyDown(object sender, KeyEventArgs e) {
            if (Keys.D1 <= e.KeyCode && e.KeyCode <= Keys.D9) {
                pixscale = e.KeyCode - Keys.D0;
                RunProgram();
            } else {
                switch (e.KeyCode) {
                    case Keys.ControlKey: RunProgram(); break;
                    case Keys.Left: NextImage(-1); break;
                    case Keys.Right: NextImage(1); break;
                    case Keys.D0: FitToScreen(); break;
                    case Keys.F11: FullScreen(); break;
                    case Keys.Escape: FullScreen(false); break;
                    case Keys.Home: pan = new Vector2(); ShowImage(); break;
                    case Keys.I: System.Diagnostics.Process.Start(@"C:\Program Files\IrfanView\i_view32.exe", Directory.GetCurrentDirectory() + "\\" + fileName); break;
                }
            }
        }

        private Form fsform;
        private bool fullscreen = false;
        private Control displayControl { get { return fullscreen ? (Control)fsform : (Control)image; } }

        private void FullScreen() { FullScreen(!fullscreen); }

        private void FullScreen(Boolean fs) {
            if (fsform == null) {
                fsform = new Form();
                fsform.Top = 0; fsform.Left = 0;
                fsform.Height = Screen.PrimaryScreen.Bounds.Height;
                fsform.Width = Screen.PrimaryScreen.Bounds.Width;
                fsform.FormBorderStyle = FormBorderStyle.None;
                resizeBackBuffer(fsform);

                fsform.MouseWheel += MouseWheelCode;
                fsform.MouseMove += MouseMoveCode;
                fsform.MouseDown += MouseDownCode;
                // fsform.Click += image_Click;
                fsform.Paint += (s, e) => ShowImage();
                fsform.KeyDown += imageSurrogate_KeyDown;

            }
            fullscreen = fs;
            if (fullscreen) fsform.Show(); else fsform.Hide();
            resizeBackBuffer(displayControl);
            //try { ShowImage(); } catch (InterruptedException) { }
        }

        private double pixscale {
            get { return _pixscale; }
            set { scale = (float)(scale * _pixscale / value); ShowImage(); }
        }

        public static void BREAK() {
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
        }

        private Drawer drawer = null;
        private void SetDraw() { if (drawer == null || drawer.IsDisposed) drawer = new Drawer(); }
        private void Draw() { if (drawer != null) drawer.Draw(); }
        internal void Draw(Conv1D c) { if (drawer != null) drawer.Draw(c); }
        internal void Draw(float[] a) { if (drawer != null) drawer.Draw(a); }

        private static int lastx, lasty;
        internal static void TMouseDownCode(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                TextBoxBase tb = (TextBoxBase)sender;
                int selstart = tb.GetCharIndexFromPosition(e.Location);
                while (selstart > 0) {
                    char c = tb.Text[selstart - 1];
                    if (('0' <= c && c <= '9') || c == '.' || c == '-') selstart--; else break;
                }
                int selend = selstart;
                while (selend < tb.Text.Length) {
                    char c = tb.Text[selend];
                    if (('0' <= c && c <= '9') || c == '.' || c == '-') selend++; else break;
                }
                if (selstart == selend) { Beep("no number selected"); return; }
                tb.SelectionStart = selstart;
                tb.SelectionLength = selend - selstart;
                lastx = e.X; lasty = e.Y;
            }
        }
        internal static void TMouseMoveCode(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                TextBoxBase tb = (TextBoxBase)sender;
                if (tb.SelectionLength == 0) return;
                int selstart = tb.SelectionStart;
                float ov;
                try {
                    ov = float.Parse(tb.SelectedText);
                } catch (Exception) {
                    Beep("text selected not number");
                    return;
                }
                float dxscale = 1 / 500f;
                float dx = e.X - lastx;
                string newpart = String.Format("{0:0.000}", ov + dx * dxscale);
                // tb.SelectedText = newpart;  // does not work, gives intermediate value of tb.Text
                string tbt = tb.Text;
                string newtext = tbt.Substring(0, selstart) + newpart + tbt.Substring(selstart + tb.SelectionLength);
                tb.Text = newtext; 
                tb.SelectionStart = selstart;
                tb.SelectionLength = newpart.Length;
                tb.Refresh();
                //tb.Parent.Refresh();
            }
            lastx = e.X; lasty = e.Y;
        }


        /// <summary>A parameter for Lanczos filter (http://en.wikipedia.org/wiki/Lanczos_resampling) (public to allow interactive field)</summary> 
        public int LancA = 2;
        /// <summary>W parameter for Lanczos filter (public to allow interactive field)</summary> 
        public double LancW = 3;
        /// <summary>square width for filter (public to allow interactive field)</summary> 
        public static double SQWidth = 1;
        /// <summary>circle/superegg power for filter (public to allow interactive field)</summary> 
        public static double CircP = 2;

        /// <summary>set up a Lanczos filter using the Form1's builtin parameters</summary> 
        internal void SetLanc() {
            try {
                if (drawer != null) {
                    drawer.DrawClear();  // in
                    drawer.col = System.Drawing.Color.Yellow;  new Lanczos(this, 2, 2, 1, 8, Effects);
                    drawer.col = System.Drawing.Color.Blue; new Lanczos(this, 2, 3, 0, 8, Effects);
                    drawer.col = System.Drawing.Color.Red;
                }
                Conv2D l = new Lanczos(this, LancA, LancW, SQWidth, 16, Effects);
                //// keep for now, worked well at a=2, w=2.9, despite being 'wrong'
                //int W = (int)Math.Floor(LancW);
                //fullTechnique = CreateEffectDirect("ImConv" + LancW, DynamicFx.ImConv,
                //    "X", W + "", "Y", W + "",
                //    "XS", (0.5 / (LancW + SQWidth)) + "", "YS", (0.5 / (LancW + SQWidth)) + "");
                int W = (int)Math.Floor(l.xwidth / 2);
                displayTechnique = Effects.CreateEffectDirect("ImConv" + LancW, DynamicFx.ImConv,
                    "X", W + "", "Y", W + "");
                displayTechnique.stepKernelXy = new Vector2(1f / l.xwidth, 1f / l.xwidth);
                displayTechnique.imageparm = l.texture;

                ShowImage();
            } catch (Exception e) {
                Beep(e);
            }
        }

        /// <summary>distortion matrix for trapezoid output, use 'trap' command</summary> 
        private Matrix trapm = Matrix.Identity;

        /// <summary>
        /// show the image iimage with current parameters, 
        /// optionally in small area ('small' parameter), mainly for timing/debug
        /// optionally show the original (unprocessed) image ('showOriginal' class flag)
        /// </summary>
        /// <param name="iimage"></param>
        /// <param name="small"></param>
        internal void ShowImage(ImageTex iimage, bool small) {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Graphics.Color.Gray);
            ShowImageNoClear(iimage, small);
        }

        /// <summary>
        /// show the image iimage with current parameters, 
        /// optionally in small area ('small' parameter), mainly for timing/debug
        /// optionally show the original (unprocessed) image ('showOriginal' class flag)
        /// </summary>
        /// <param name="iimage"></param>
        /// <param name="small"></param>
        internal void ShowImageNoClear(ImageTex iimage, bool small) {
            if (iimage == null) return;
            // Console.WriteLine("ShowImage " + kkkk++);
            if (!showOriginal) processedImage = iimage;
            if (displayTechnique == null) { Form1.Beep(new Exception("null displayTechnique")); return; }
            Effect effect = displayTechnique.effect;
            if (effect == null) { Form1.Beep(new Exception("null effect")); return; }
            programBox.ForeColor = System.Drawing.Color.Black;

            if (GraphicsDevice.Viewport.Width != displayControl.ClientSize.Width && !fullscreen) { Form1.BREAK(); }  // debug

            Viewport save = GraphicsDevice.Viewport;
            if (small) GraphicsDevice.Viewport = SmallVP;
            GraphicsDevice.Viewport = xvp;

            int ssizeX = xvp.Width;// -xvp.X;
            int ssizeY = xvp.Height;// -xvp.Y;

            float isizeX = iimage.Width;
            float isizeY = iimage.Height;

            // now send parameters and final display technique to shader
            float hh = 0.5f * scale;
            Matrix mat;
            if (rotate) {
                float rr = isizeX * ssizeX / (isizeY * ssizeY);
                float xs = hh, ys = hh * rr;
                if (rr < 1) { xs = hh / rr; ys = hh; }
                mat = new Matrix(0, -ys, 0, 0, -xs, 0, 0, 0, 0, 0, 1, 0, pan.Y + 0.5f, -pan.X + 0.5f, 0, 1);
                _pixscale = Math.Min(ssizeX / isizeY, ssizeY / isizeX) / scale;
            } else {
                float rr = isizeX * ssizeY / (isizeY * ssizeX);
                float xs = hh, ys = hh * rr;
                if (rr < 1) { xs = hh / rr; ys = hh; }
                mat = new Matrix(xs, 0, 0, 0, 0, -ys, 0, 0, 0, 0, 1, 0, pan.X + 0.5f, pan.Y + 0.5f, 0, 1);
                _pixscale = Math.Min(ssizeX / isizeX, ssizeY / isizeY) / scale;
            }
            if (!showOriginal) {
                mat = mat * trapm;
            }
            effect.Parameters["TexProj"].SetValue(mat);
            effect.Parameters["isize"].SetValue(new Vector2(isizeX, isizeY));
            effect.Parameters["stepImageXy"].SetValue(iimage.stepImageXy);
            displayTechnique.SetParameters();
            if (iimage != null) effect.Parameters["image"].SetValue(iimage.texture);
            // effect.Parameters["imageparm"].SetValue(Lanczos.texture);
            // ExtraParameters(effect);

            //GraphicsDevice.SetRenderTarget(0, screenTarget);

            Effects.RunEffect(effect);

            // and display the output
            //Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Microsoft.Xna.Framework.Rectangle r = new Microsoft.Xna.Framework.Rectangle(xvp.X, xvp.Y, xvp.Width, xvp.Height);
            try {
                GraphicsDevice.Present(r, r, displayControl.Handle);
            } catch (DeviceLostException) {
                RecoverDevice();
            }
            this.Text = fileName + ":  " + (showOriginal ? "original" : "processed") + " - ShaderImage .. scale=" + _pixscale + iimage.fileInfo;

            GraphicsDevice.Viewport = save;

            // housekeeping
            uint esck = Utils.GetAsyncKeyState(Utils.VK_ESCAPE);
            bool esc = (esck & 0x8000) != 0;
            if (esc) throw new InterruptedException();
            GC.Collect(); // needed to clear up framework objects and release associated graphics resources
        }


        ///<summary>recover device after DeviceLostException</summary>
        private void RecoverDevice() {
            Console.WriteLine("device lost noticed");
            Effects.Kill();  // just in case
            Effects = null;  // should be rest soon, but just in case
            displayTechnique = null;
            fileImage = null;
            processedImage = null;
            ImageTex.i1 = ImageTex.i2 = ImageTex.i3 = ImageTex.i4 = null;
            pres = null;  // so initialize works
            InitializeRenderFramework(image);
            LoadImage();
        }

    }  // Form1


    public class InterruptedException : Exception { }

    class ToolStripTextBoxX : ToolStripTextBox {
        private FieldInfo fieldInfo;
        private Form1 form1;
        private static List<ToolStripTextBoxX> boxes = new List<ToolStripTextBoxX>();
        internal ToolStripTextBoxX(string text, FieldInfo fieldInfo, Form1 form1)
            : base(text) {
            AutoSize = false;
            Text = text;
            Width = 60;
            MouseDown += MouseDownCode;
            MouseMove += MouseMoveCode;
            TextChanged += TextChangedCode;
            this.fieldInfo = fieldInfo;
            this.form1 = form1;
            boxes.Add(this);
        }
        private int lastx = 0, lasty = 0;
        private void TextChangedCode(object sender, EventArgs e) {
            if (fieldInfo.FieldType == typeof(int)) fieldInfo.SetValue(form1, IntVal);
            else if (fieldInfo.FieldType == typeof(float)) fieldInfo.SetValue(form1, FloatVal);
            else if (fieldInfo.FieldType == typeof(double)) fieldInfo.SetValue(form1, DoubleVal);
            else Form1.Beep("bad field type");
            form1.SetLanc();
        }
        private void MouseDownCode(object sender, MouseEventArgs e) {
            lastx = e.X; lasty = e.Y;
        }
        private void MouseMoveCode(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                float dxscale = 1 / 100f;
                float dx = e.X - lastx;
                Text = String.Format("{0:0.000}", FloatVal + dx * dxscale);
                this.Parent.Refresh();
            }
            lastx = e.X; lasty = e.Y;
        }
        private void RefreshText() {
            Text = fieldInfo.GetValue(form1) + "";
            //if (fieldInfo.FieldType == typeof(int)) Text = fieldInfo.GetValue(form1) + "";
            //else if (fieldInfo.FieldType == typeof(float)) fieldInfo.SetValue(form1, FloatVal);
            //else if (fieldInfo.FieldType == typeof(double)) fieldInfo.SetValue(form1, DoubleVal);
            //else Form1.Beep("bad field type");
        }
        internal static void RefreshAll() {
            foreach (ToolStripTextBoxX b in boxes) b.RefreshText();
        }
        private int IntVal { get { return Text.Int(); } }
        private float FloatVal { get { return Text.Float(); } }
        private double DoubleVal { get { return Text.Double(); } }

    }

}
/***
 * Notes:
 * basic RenderTarget technique
 * optimization, avoid intermediate RenderTarget/textures
 *   needs either dyanamic generation of shader or metashader
 * optimization, avoid use of more RenderTargets than necessary
 *   but we do need >1 as GetTexture() is effectively just a reference to memory
 *   oddities with render speed, sometimes adding extra display seems to help speed
 * also experiments with C# console   
 *   still issue with multiple cascaded subclasses
 * Vector4 option for computation ~ much slower
***/