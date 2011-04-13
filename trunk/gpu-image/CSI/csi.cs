using System;
using System.IO;

class RunCSI {
    const string caption = "CSI Simple C# Interpreter",
                       prompt = "# ";
    
    public static void XMain(string [] args) {
        Interpreter.Console = new TextConsole();
        Utils.Write(caption+"\n"+prompt);        
        Interpreter interp = new Interpreter();
        string defs = args.Length > 0 ? args[0] : "csi.csi";
        interp.ReadIncludeFile(defs);        
        while (interp.ProcessLine(Utils.ReadLine())) 
            Utils.Write(prompt);
    }
}

class TextConsole : IConsole {
    public string ReadLine() {
        return Console.In.ReadLine();
    }    
    
    public void Write(string s) {
        Console.Write(s);
    }
}
