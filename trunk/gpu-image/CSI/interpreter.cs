// // CSharp Interpreter CSI taken from http://www.codeproject.com/KB/cs/csi.aspx
// interpreter.cs
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

public interface IConsole {
    string ReadLine();
    void Write(string s);
}

public class Utils /*: Images.ImageTex*/ {  // TODO: remember (and document) why this was a subclass
    static Type lastClass = null;
    
    public static void MInfo(object ctype, string mname) {
        Type t;
        if (ctype == null) {
            if (lastClass != null)
                ctype = lastClass;
            else
                return;
        }
        if (ctype is String) {
            string cname = (string)ctype;
            if (cname.Length < 7 || cname.Substring(0,7) != "System.")
                cname = "System." + cname;
            t = Type.GetType(cname);
             if (t == null)  throw(new Exception("is not a type"));
        } else
            t = (Type)ctype;
        lastClass = t;
        try {
            string lastName = "";
            int k = 0;
            if (! t.IsClass && ! t.IsInterface)
                throw(new Exception("is not a class, struct or interface"));            
            foreach (MethodInfo mi in t.GetMethods()) {
                if (mi.IsPublic && mi.DeclaringType == t) 
                    if (mname == null) {
                        if (mi.Name != lastName && mi.Name.IndexOf('_') == -1) {
                            lastName = mi.Name;
                            Write(lastName);
                            if (++k == 5) {
                                Print();
                                k = 0;
                            } else
                                Write(" ");
                        }
                    } else {
                        if (mi.Name == mname) {
                            string proto = mi.ToString();
                            proto = proto.Replace("System.","");
                            if (mi.IsStatic)
                                proto = "static " + proto;
                            if (mi.IsVirtual)
                                proto = "virtual " + proto;
                            Print(proto);                            
                        }                            
                    }
            }
            if (k > 0)
                Print();
        } catch(Exception e) {
            Print("Error: " + ctype,e.Message);
        }
    }    
 
    public static void Printl(IEnumerable c) {
        foreach(object o in c) {
            if (o != null) Write(o.ToString());
                        else Write("<null>");
            Write(" ");
        }
         Write("\n");     
    }
    // a very convenient function for quick output ('Print' is easier to type than 'Console.WriteLine')
    public static void Print(params object []obj) {
        Printl(obj);
    }
    
    public static string ReadLine() {
        return Interpreter.Console.ReadLine();
    }    
    
    public static void Write(string s) {
        Interpreter.Console.Write(s);
    }
    
}

public class CodeChunk : Utils {
    
    // the generated assemblies will have to override this method
    public virtual void Go(Hashtable V) {
    }
    
    // here's the template used to generate the assemblies
    public const string Template =
         @"$USES$
       class CsiChunk : CodeChunk { 
        public override void Go(Hashtable V) {
          $BODY$;
        }
      }";       
    
    public static void Instantiate(Assembly a, Hashtable table) {
        long st = 0;
        try {
            st = Environment.TickCount;
            CodeChunk chunk = (CodeChunk)a.CreateInstance("CsiChunk");
            long ddt = Environment.TickCount - st; Write("time createinstance " + ddt + "\n"); st = Environment.TickCount;
            chunk.Go(table);
        }  catch(Exception ex) {
            Print(ex.GetType() + " was thrown: " + ex.Message);
        }
        long dt = Environment.TickCount - st;
        Write("time = " + dt + "   ...   " + Interpreter.rawLine + "\n");

    }
}

public class CsiFunctionContext : Utils { //*
    public Hashtable V;    

    public const string Template =
         @"$USES$
       public class $CLASS$ : CsiFunctionContext { 
         public $BODY$
      }";       
    
    public static void Instantiate(Assembly a, Hashtable table, string className, string funName) {
        try {
            CsiFunctionContext dll = (CsiFunctionContext)a.CreateInstance(className);
            dll.V = table;
            table[className] = dll;
        }  catch(Exception ex) {
            Print(ex.GetType() + " was thrown: " + ex.Message);
        }	    
    }    
}

public class Interpreter {
    Hashtable varTable = new Hashtable();
    string namespaceString = "";
    ArrayList referenceList = new ArrayList();
    Dictionary<string, string> providerOptions = new Dictionary<string, string>();
    
    CSharpCodeProvider prov;
    // ICodeCompiler compiler;      
    public static IConsole Console;
    
    MacroSubstitutor macro = new MacroSubstitutor();    
    
    public Interpreter() {
        providerOptions.Add("CompilerVersion", "v3.5");
        prov = new CSharpCodeProvider(providerOptions);
        AddNamespace("System");
        AddNamespace("System.Collections");
        AddReference("system.dll"); 
        SetValue("interpreter",this);
        // get the file name of this executable
        Assembly thisAssembly = Assembly.GetAssembly(typeof(Interpreter));
        // AddReference(Path.GetFileName(thisAssembly.CodeBase));
        AddReference(thisAssembly.Location);
        // compiler = prov.CreateCompiler();     
    }
    
    public void ReadIncludeFile(string file) {
        if (File.Exists(file)) 
            using(TextReader tr = File.OpenText(file)) {
                while (ProcessLine(tr.ReadLine()))
                    ;
            }
    }
    
    public void SetValue(string name, object val) {
        varTable[name] = val;
    }
    
    StringBuilder sb = new StringBuilder();    
    int bcount = 0;

    public static string rawLine = "";
    public bool ProcessLine(string line) {
        rawLine = line;
     // Statements inside braces will be compiled together
        if (line == null)
            return false;
        if (line == "")
            return true;        
        if (line[0] == '/') {
            ProcessCommand(line);
            return true;
        }
        sb.Append(line);
        // ignore {} inside strings!  Otherwise keep track of our block level
        bool insideQuote = false;
        for (int i = 0; i < line.Length; i++) {
            if (line[i] == '\"')
                insideQuote = ! insideQuote;
            if (! insideQuote) {
                if (line[i] == '{') bcount++; else
                if (line[i] == '}') bcount--;
            }
        }
        if (bcount == 0) {            
            string code = sb.ToString();
            sb = new StringBuilder();
            if (code != "")
                ExecuteLine(code);            
        }
        return true;
    }
    
    static Regex cmdSplit = new Regex(@"(\w+)($|\s+.+)");
    static Regex spaces = new Regex(@"\s+");
    
    void ProcessCommand(string line) {
        Match m = cmdSplit.Match(line);
        string cmd = m.Groups[1].ToString();
        string arg  = m.Groups[2].ToString().TrimStart(null);
        switch(cmd) {
        case "n":
            AddNamespace(arg); 
            break;
        case "r":
            AddReference(arg);
            break;
        case "v":
            foreach(string v in varTable.Keys)
                Utils.Print(v + " = " + varTable[v]);
            break;
        default: 
            // a macro may be used as a command; the line is split up and
            // and supplied as arguments.
            // For macros taking one argument, the whole line is supplied.
            MacroEntry me = macro.Lookup(cmd);
            if (me != null && me.Parms != null) {
                string[] parms;
                if (me.Parms.Length > 1)
                    parms = spaces.Split(arg);
                else
                    parms = new string[] { arg };
                string s = macro.ReplaceParms(me,parms);                
                ExecuteLine(s);
            } else
                Utils.Print("unrecognized command, or bad macro");
            break;
        }
    }
    
    // the actual dynamic type of an object may not be publically available
    // (e.g. Type.GetMethods() returns an array of RuntimeMethodInfo)
    // so we look for the first public base class.
    Type GetPublicRuntimeType(object symVal) {                
        Type symType = null;
        if (symVal != null) {
            symType = symVal.GetType();
            while (! symType.IsPublic)
                symType = symType.BaseType;            
        }
        return symType;        
    }
    
    Regex word = new Regex(@"\$\w+");
    Regex assignment = new Regex(@"\$\w+\s*=[^=]");  
    
    // 'session variables' like $x will be replaced by ((LastType)V["x"]) where
    // LastType is the current type associated with the last value of V["x"].    
    string MassageInput(string s) {
        foreach(Match m in word.Matches(s)) {
            string sym = m.Value.Substring(1);       // i.e. without the '$'
            string symRef = "V[\"" + sym + "\"]";     // will index our hashtable
            // are we followed by an assignment operator?
            Match lhs = assignment.Match(s,m.Index);
            bool was_assignment = lhs != Match.Empty && lhs.Index == m.Index;           
            Type symType = GetPublicRuntimeType(varTable[sym]);
             // unless we're on the LHS, try to strongly type this variable reference.
            if (symType != null && ! was_assignment)
                symRef = "((" + symType.ToString() + ")" + symRef + ")";            
            s = word.Replace(s,symRef,1,m.Index);
        }        
        return s;
    }
    
    static Regex funDef = new Regex(@"\s*[a-zA-Z]\w*\s+([a-zA-Z]\w*)\s*\(.*\)\s*{");
    static int nextAssembly = 1;

    // optimize repeated requests
    string lastCodeStr = null;
    Assembly lastAssembly = null;

    void ExecuteLine(string codeStr) {
        if (codeStr == lastCodeStr) {
            CodeChunk.Instantiate(lastAssembly, varTable);
            return;
        }
        // at this point we either have a line to be immediately compiled and evaluated,
        // or a function definition.
        // long st = Environment.TickCount;  // interpretation time is around 250 millesec
        string className=null,assemblyName=null,funName=null;
        Match funMatch = funDef.Match(codeStr);
        bool hasName = funMatch != Match.Empty;
        if (hasName) {
            funName = funMatch.Groups[1].ToString();
            macro.RemoveMacro(funName);
            System.Console.WriteLine(funName);
            className = "Csi" + nextAssembly++;
            assemblyName = className + ".dll";                        
            codeStr = codeStr.Insert(funMatch.Groups[1].Index,"_");
        }
        codeStr = macro.ProcessLine(codeStr);
        if (codeStr == "")  // may have been a prepro statement!
            return;
        CompilerResults cr = CompileLine(codeStr,hasName,assemblyName,className);
        if (cr != null) {
            Assembly ass = cr.CompiledAssembly;
            if (!hasName) {
                CodeChunk.Instantiate(ass, varTable);
                lastCodeStr = codeStr;
                lastAssembly = ass;
            }  else {
                CsiFunctionContext.Instantiate(ass, varTable, className, funName);
                macro.AddMacro(funName, "$" + className + "._" + funName, null);
                AddReference(assemblyName);
            }
        }
        // long dt = Environment.TickCount - st; 
        // Console.Write("alltime = " + dt + "\n");

    }
    
    CompilerResults CompileLine(string codeStr,bool hasName,string assemblyName, string className) {
        codeStr = MassageInput(codeStr); 
        CompilerParameters cp = new CompilerParameters();
        if (hasName)
            cp.OutputAssembly = assemblyName;
        else
            cp.GenerateInMemory = true;        
        
        foreach(string r in referenceList)
            cp.ReferencedAssemblies.Add(r);
        
        string finalSource = CodeChunk.Template;
        if (hasName)
            finalSource = CsiFunctionContext.Template;
        finalSource = finalSource.Replace("$USES$",namespaceString);
        finalSource = finalSource.Replace("$BODY$",codeStr);                  
        if (hasName)
            finalSource = finalSource.Replace("$CLASS$",className);
        CompilerResults cr = prov.CompileAssemblyFromSource(cp, finalSource);
        // CompilerResults cr = compiler.CompileAssemblyFromSource(cp, finalSource);        
        if (! ShowErrors(cr,codeStr))
            return cr;
        else
            return null;
    }
    
    public void AddNamespace(string ns) {
        namespaceString = namespaceString + "using " + ns + ";\n";        
    }
    
    public void AddReference(string r) {
        referenceList.Add(r);
    }
    
    bool ShowErrors(CompilerResults cr, string codeStr) {
        if (cr.Errors.HasErrors) {
            StringBuilder sbErr;
            sbErr = new StringBuilder("Compiling string: ");
            sbErr.AppendFormat("\'{0}\'", codeStr);
            sbErr.Append("\n\n");
            foreach(CompilerError err in cr.Errors) {
                sbErr.Append(err.ErrorText);
                sbErr.Append("\n");
            }
            Utils.Print(sbErr.ToString());		
            return true;
        }	       
        else return false;
    }
}

