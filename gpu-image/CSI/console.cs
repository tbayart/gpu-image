// CSharp Interpreter CSI taken from http://www.codeproject.com/KB/cs/csi.aspx
using System;
using System.IO;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;

public delegate void StringHandler(string line);

class ConsoleTextBox : RichTextBox { 
    GuiConsoleForm parent;
    
    public ConsoleTextBox(GuiConsoleForm parent_) {
        parent = parent_;
        KeyDown += XKeyDown;
    }


    private void XKeyDown(object sender, KeyEventArgs e) {
        string line = null;
        if (e.KeyCode == Keys.Enter) {
            if (SelectionLength == 0) {
                int lineNo = GetLineFromCharIndex(SelectionStart);
                if (lineNo < Lines.Length) {
                    line = Lines[lineNo];
                }
            } else {
                // line = SelectedText.Replace("\n>", " ");  // ?? this should be done where prompt is set and use prompt
                line = SelectedText;
            }

            if (line != null) {
                parent.DelayedExecute(line);
                if (SelectionStart != Text.Length) e.Handled = true;  // do not let Enter insert CR unless at very end
            }
        }
    }

    // execute the selection line.  If substitute is true, replace selection with $VV
    public void ExecuteSelectionLine(bool substitute) {
        int lineNo = GetLineFromCharIndex(SelectionStart);
        string line = Lines[lineNo];
        if (substitute && SelectionLength > 0) {
            int lineStartIndex = GetFirstCharIndexFromLine(lineNo);
            line = line.Substring(0, SelectionStart - lineStartIndex) + "$VV" + line.Substring(SelectionStart + SelectionLength - lineStartIndex);
            RunCsi.interp.SetValue("VV", float.Parse(SelectedText));
            Console.WriteLine(".>. " + line);
        }
        parent.quiet = true;
        RunCsi.interp.ProcessLine(GuiConsoleForm.CleanLine(line));
        // parent.DelayedExecute(line);
    }


/*        
    protected override bool IsInputKey(Keys keyData) {
        string line = null;
        if (keyData == Keys.Enter) {
            if (SelectionLength == 0) {
                int lineNo = GetLineFromCharIndex(SelectionStart);
                if (lineNo < Lines.Length) {
                    line = Lines[lineNo];
                }
            } else {
                line = SelectedText.Replace("\n>", " ");  // ?? this should be done where prompt is set and use prompt
            }

            if (line != null) {
                parent.DelayedExecute(line);
                return false;
            }
       }
       return base.IsInputKey(keyData);
    }
 * */
}

public class GuiConsoleForm : Form 	
{     
    ConsoleTextBox textBox;
    static string prompt;
    Timer timer = new Timer();
    string currentLine;
    StringHandler stringHandler;    
    
    public GuiConsoleForm(string caption, string cmdPrompt, StringHandler h) 
    {        
        Text = caption;
        prompt = cmdPrompt;
        stringHandler = h;
        textBox = new ConsoleTextBox(this);
        textBox.Dock = DockStyle.Fill;
        textBox.Font = new Font("Tahoma",10,FontStyle.Bold);
        textBox.WordWrap = false;
        
        Width = 750;
        Size = new Size(467, 400);

        timer.Interval = 50;
        timer.Tick += new EventHandler(Execute);
        
        this.Controls.Add(textBox);

        // quick interaction with code, including realtime right mouse drag
        textBox.MouseDown += Images.Form1.TMouseDownCode;
        textBox.MouseMove += Images.Form1.TMouseMoveCode;  // get changes on screen
        textBox.MouseMove += (s, e) => { if (e.Button == MouseButtons.Right) textBox.ExecuteSelectionLine(true); }; // and executed
        textBox.DoubleClick += (s, e) => textBox.ExecuteSelectionLine(false);
    }

    public static string CleanLine(string lline) {
        string[] lines = lline.Split('\n');
        string cl = "";
        foreach (string l in lines) {
            string line = l;
            while (line.IndexOf(prompt) == 0)
                line = line.Substring(prompt.Length);
            if (line.Contains(" ... ")) line = line.Substring(line.IndexOf(" ... ") + 5).Trim();
            cl += line;
        }
        return cl;
    }
    
    public void DelayedExecute(string lline) {
        currentLine = CleanLine(lline);
        timer.Start();
    }   
     
    void Execute(object sender,EventArgs e) {
        timer.Stop();
        stringHandler(currentLine);
        Write(prompt);
        quiet = false;
    }

    public bool quiet = false;
    public void Write(string s) {
        if (!quiet) textBox.AppendText(s);
    }
    
    public RichTextBox TextBox {
        get { return textBox; }
    }
    
}



