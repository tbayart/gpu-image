// CSharp Interpreter CSI taken from http://www.codeproject.com/KB/cs/csi.aspx
using System;
using System.IO;
using System.Windows.Forms;

public class RunCsi{
    const string caption = "CSI Simple C# Interpreter (Steve Donovan)",
                       prompt = "> ";
    
    public static Interpreter interp;  // public to simplify poking $VV for realtime interactive use
    
    static void ProcessLine(string line) {
        interp.ProcessLine(line);
    }    
    
    public static Interpreter XMain(string[] args) {
        GuiConsoleForm form = new GuiConsoleForm(caption,prompt,new StringHandler(ProcessLine));
        GuiConsole console = new GuiConsole(form);
        console.Write(caption+"\n"+prompt);        
        string defs = args.Length > 0 ? args[0] : "csigui.csi";        
        Interpreter.Console = console;
        interp = new Interpreter();
        interp.ReadIncludeFile(defs);        
        interp.SetValue("form",form);
        interp.SetValue("text",form.TextBox);
        form.Show();
        return interp;
        // Application.Run(form);
    }   

}

class GuiConsole : IConsole {
    GuiConsoleForm form;
    
    public GuiConsole(GuiConsoleForm f) {
        form = f;
    }
    
    public string ReadLine() {
        return "";
    }
    
    public void Write(string s) {
        form.Write(s);
    }       
}
