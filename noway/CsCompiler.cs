using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
namespace Noway {
    public class CSharpCompiler : ICompiler {
        public CSharpCompiler() {}

        private Dictionary<Type, TypeData> conversions = new Dictionary<Type, TypeData>() {
            { typeof(bool), SysLib.BoolType },
            { typeof(byte), SysLib.ByteType },
            { typeof(char), SysLib.CharType },
            { typeof(double), SysLib.DoubleType },
            { typeof(float), SysLib.FloatType },
            { typeof(long), SysLib.LongType },
            { typeof(object), SysLib.ObjectType },
            { typeof(short), SysLib.ShortType },
            { typeof(string), SysLib.StringType },
            { typeof(void), SysLib.VoidType },
            { typeof(int), SysLib.IntType },
            { typeof(IEnumerator<>), SysLib.IteratorType },
            { typeof(IEnumerable<>), SysLib.IterableType },
            { typeof(List<>), SysLib.ListType },
            { typeof(Console), SysLib.ConsoleType }
        };
        private Dictionary<string, string> sysLibMethodNameConversions = new Dictionary<string, string>() {
            { "sys.string.sub", "Substring" },
        };
        private Type GetType(string name) {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies()) {
                var type = a.GetType(name);
                if(type != null) {
                    return type;
                }
            }
            return null;
        }
        public string FromCsType(Type type) {
            var ret = "";
            if(type.IsGenericType) {
                var args = type.GetGenericArguments();
                ret = type.FullName + '<' + ToCsString(args[0]);
                for(int i = 1; i < args.Length; i++)
                    ret += ", " + ToCsString(args[i]);
                ret += '>';
            } else {
                ret = type.ToString();
            }
            foreach(var p in conversions) {
                if(p.Key.FullName == ret) {
                    ret = p.Value.name;
                    break;
                }
            }
            return ret;
        }
        public Type ToCsType(string fullName) {

            //shorthand array declaration
            if(fullName.LastIndexOf("[]") == fullName.Length - 2) {
                return ToCsType(fullName.Substring(0, fullName.Length - 2)).MakeArrayType();
            }

            //if generic find generic name, ie: sys.list<T> -> sys.list
            var index = fullName.IndexOf('<');
            var name = index >= 0 ? fullName.Substring(0, index) : fullName;

            //if a system type, swap it to the c# type
            foreach(var p in conversions) {
                if(p.Value.name == name) {
                    name = p.Key.ToString();
                    var index2 = name.IndexOf('`');
                    if(index2 >= 0) {
                        name = name.Substring(0, index2);
                    }
                    break;
                }
            }
            //any generic types
            if(index >= 0) {
                var args = fullName.Substring(index + 1, fullName.LastIndexOf('>') - index - 1).Replace(" ", "").Split(',');
                
                //default array declaration: sys.arr<T>
                if(name == SysLib.ArrayType.name) {
                    return ToCsType(args[0]).MakeArrayType();
                }

                //if not array return the generic version
                var genericType = GetType(name + '`' + args.Length);
                if(genericType == null) return null;
                return genericType.MakeGenericType(args.Select(t => ToCsType(t)).ToArray());
            } else {
                //if not generic, directly return c# type
                return GetType(name);
            }
        }
        public string ToCsString(Type type) {
            var ret = "";
            if(type.IsGenericType) {
                var args = type.GetGenericArguments();
                ret = type.ToString().Substring(0, type.ToString().IndexOf('`')) + '<' + ToCsString(args[0]);
                for(int i = 1; i < args.Length; i++)
                    ret += ", " + ToCsString(args[i]);
                ret += '>';
            } else {
                ret = type.ToString();
            }
            switch(ret) {
                case "System.Void": return "void";
                case "System.Int32": return "int";
                case "System.Int64": return "long";
                case "System.Single": return "float";
                case "System.Double": return "double";
                case "System.Bool": return "bool";
                case "System.Int16": return "short";
                case "System.Byte": return "byte";
                case "System.Object": return "object";
                case "System.String": return "string";
            }
            return ret;
        }
        private static string dependencies = @"
    public class Dependencies {
		public static System.Collections.Generic.IEnumerable<int> To(int a, int b) {
			if(a < b)
                for(int i = a; i <= b; i++)
                    yield return i;
            else
                for(int i = a; i >= b; i--)
                    yield return i;
		}
        private static System.Random rng = new System.Random();
        public static int RandInt(int min, int max) {
            return rng.Next(min, max + 1);
        }
	}";
        public void Compile(ProgramContext program) {
            var s = "namespace Test {\n\tpublic class Program {\n\t\tpublic static void Main(string[] args) {" + GenCode(program.main) + "\n\t\t}\n\t}" + dependencies + "\n}";
            var srcpath = @"C:\Users\Justin\Documents\Programming\Noway\out";
            var cscpath = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe";
            var name = "Game";
            File.WriteAllText(srcpath  + @"\src.txt", s);
            System.Threading.Thread.Sleep(1000);
            var compiler = new ProcessStartInfo() {
                WorkingDirectory = srcpath,
                Domain = srcpath,
                FileName = cscpath,
                UseShellExecute = true,
                CreateNoWindow = false,
                Arguments = $"-out:{name}.exe *.txt"
            };
            Process.Start(compiler).WaitForExit();

            var exe = new ProcessStartInfo() {
                WorkingDirectory = srcpath,
                Domain = srcpath,
                FileName = srcpath + $"\\{name}.exe",
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(exe);
        }
        public bool ValidateType(string fullName, out TypeData td) {
            td = null;
            var type = ToCsType(fullName);
            if(type != null) {
                td = new TypeData() {
                    name = fullName
                };
                return true;
            }
            return false;
        }
        public bool ValidateMember(MemberContext member, out MemberData md) {
            md = null;
            var t = ToCsType(member.owner.type);
            var type = t?.GetProperty(member.memberName)?.PropertyType ?? t?.GetField(member.memberName)?.FieldType;
            if(type == null) return false;
            
            md = new MemberData() {
                name = member.memberName,
                returnType = FromCsType(type),
            };
            return true;
        }
        private Dictionary<Tuple<string, IEnumerable<string>>, CallContext> validatedCalls = new Dictionary<Tuple<string, IEnumerable<string>>, CallContext>();
        public bool ValidateCall(CallContext call, out MethodData md) {
            var owner = ToCsType(call.owner.type);
            var args = call.arguments.Select(c => ToCsType(c.type)).ToArray();

            md = new MethodData() {
                name = call.methodName
            };

            switch(call.methodName) {
                case "_getIndex": {
                    if(owner.IsArray && args.Length == 1 && args[0] == typeof(int)) {
                        md.returnType = FromCsType(owner.GetElementType());
                        md.parameterNames.Add("index");
                        md.parameterTypes.Add(SysLib.IntType.name);
                        return true;
                    } else {
                        var props = owner.GetProperties().Where(p => p.GetIndexParameters().Select(p2 => p2.ParameterType).SequenceEqual(args)).ToArray();
                        
                    }
                    break;
                }
                case "_setIndex": {
                    if(owner.IsArray && args.Length == 2 && args[0] == typeof(int) && args[1] == owner.GetElementType()) {
                        md.returnType = SysLib.VoidType.name;
                        md.parameterNames = new List<string>() { "index", "value" };
                        md.parameterTypes = new List<string>() {
                            SysLib.IntType.name,
                            FromCsType(owner.GetElementType())
                        };
                        return true;
                    }
                    break;
                }
            }
            md.returnType = "sys.void";
            if(args.Any(a => a == null) || owner == null) return false;
            var method = owner.GetMethod(call.methodName, args.ToArray());
            if(method == null) return false;
            md.returnType = FromCsType(method.ReturnType);
            return true;
        }
        private string indent = "\n\t\t";
        private string GenCode(TypedContext context) {
            if(context is BlockContext) {
                string ret = "{";
                string oldIndent = indent;
                indent += "\t";
                (context as BlockContext).body.ForEach(a => ret += indent + GenCode(a) + ';');
                indent = oldIndent;
                return ret += indent + "}";
            }
            else if(context is FunctionContext) {
                var c = context as FunctionContext;
                string ret = "";
                string oldIndent = indent;
                indent += "\t";
                if(c.body is BlockContext) {
                    var body = c.body as BlockContext;
                    for(int i = 0; i < body.body.Count - 1; i++) {
                        ret += indent + GenCode(body.body[i]) + ';';
                    }
                    ret += indent + (c.type == "sys.void" || (c.parent.parent as ProgramContext).main == c ? "" : "return ") + GenCode(body.body.Last()) + ';';
                }
                indent = oldIndent;
                return ret;
            }
            else if(context is BreakContext) {
                return "break";
            }
            else if(context is StaticTypeContext) {
                var c = context as StaticTypeContext;
                return c.type;
            }
            else if(context is ArrayInitContext) {
                var c = context as ArrayInitContext;
                return $"new {ToCsString(ToCsType(c.type).GetElementType())}[{GenCode(c.length)}]";
            }
            else if(context is ListContext) {
                var c = context as ListContext;
                var ret = $"new {ToCsString(ToCsType(c.type))}()";
                if(c.content.Count > 0)
                    ret += $" {{{c.content.Select(c => GenCode(c)).AddCommas()}}}";
                return ret;
            }
            else if(context is MemberContext) {
                var c = context as MemberContext;
                return $"{GenCode(c.owner)}.{c.memberName}";
            }
            else if(context is CallContext) {
                var c = context as CallContext;
                switch(c.methodName) {
                    case "_getIndex": return $"{GenCode(c.owner)}[{c.arguments.Select(a => GenCode(a)).AddCommas()}]";
                    case "_setIndex": return $"{GenCode(c.owner)}[{c.arguments.SkipLast(1).Select(a => GenCode(a)).AddCommas()}] = {GenCode(c.arguments.Last())}";
                }
                string args = c.arguments.Select(a => GenCode(a)).AddCommas();
                switch(c.owner.type.Before("<") + '.' + c.methodName) {
                    case "sys.object.hash": return $"{GenCode(c.owner)}.GetHashCode()";
                    case "sys.object.toStr": return $"{GenCode(c.owner)}.ToString()";

                    case "sys.string.upper": return $"{GenCode(c.owner)}.ToUpper()";
                    case "sys.string.lower": return $"{GenCode(c.owner)}.ToLower()";
                    case "sys.string.sub": return $"{GenCode(c.owner)}.Substring({args})";
                    case "sys.string.split": return $"{GenCode(c.owner)}.Split({args})";
                    case "sys.string.replace": return $"{GenCode(c.owner)}.Replace({args})";
                    case "sys.string.find": return $"{GenCode(c.owner)}.IndexOf({args}";

                    case "sys.list.add": return $"{GenCode(c.owner)}.Add({args})";
                    case "sys.list.remove": return $"{GenCode(c.owner)}.Remove({args})";
                    case "sys.list.len": return $"{GenCode(c.owner)}.Count";

                    case "sys.arr.len": return $"{GenCode(c.owner)}.Length";

                    case "sys.rand.nextInt": return $"Dependencies.RandInt({args})";

                    case "sys.console.print": return $"System.Console.Write({args})";
                    case "sys.console.println": return $"System.Console.WriteLine({args})";
                    case "sys.console.readln": return "System.Console.ReadLine()";
                    case "sys.console.wait": return "System.Console.ReadKey()";
                    case "sys.console.clear": return "System.Console.Clear()";
                }
                return $"{GenCode(c.owner)}.{c.methodName}({args})";
            }
            else if(context is FromContext) {
                var c = context as FromContext;
                return $"{GenCode(c.reference)} in {GenCode(c.body)}";
            }
            else if(context is IfContext) {
                var c = context as IfContext;
                return $"if({GenCode(c.condition)}) {GenCode(c.body)}";
            }
            else if(context is ElseContext) {
                var c = context as ElseContext;
                var prev = GenCode(c.chainParent);
                if(prev.Last() != '}') prev += ';';
                return $"{prev} else {GenCode(c.body)}";
            }
            else if(context is LoopContext) {
                var c = context as LoopContext;
                if(c.condition is FromContext) {
                    return $"foreach({GenCode(c.condition)}) {GenCode(c.body)}";
                } else if(c.condition.type == SysLib.BoolType.name) {
                    return $"while({GenCode(c.condition)}) {GenCode(c.body)}";
                }
                return $"for({GenCode(c.condition)}) {GenCode(c.body)}";
            }
            else if(context is AssignContext)  {
                var c = context as AssignContext;
                return GenCode(c.variable) + " = " + GenCode(c.value);
            }
            else if(context is VarDecContext)  {
                var c = context as VarDecContext;
                return $"{ToCsString(ToCsType(c.type))} {c.name}";
            }
            else if(context is ArrayContext) {
                var c = context as ArrayContext;
                var ret =  $"new {ToCsString(ToCsType(c.type))} {{";
                foreach(var e in c.content) {
                    ret += GenCode(e) + ", ";
                }
                ret = ret.Substring(0, ret.Length - 2) + '}';
                return ret;
            }
            else if(context is VarContext) {
                var c = context as VarContext;
                return c.variable.name;
            }
            else if(context is ParenContext) {
                var c = context as ParenContext;
                return $"({GenCode(c.interior)})";
            }
            else if(context is ConstContext) {
                var c = context as ConstContext;
                if(c.type == "sys.string") return $"\"{c.value}\"";
                if(c.type == "sys.char") return $"'{c.value}'";
                if(c.type == "sys.bool") return c.value.ToString().ToLower();
                return c.value.ToString();
            }
            else if(context is ConvertContext) {
                var c = context as ConvertContext;
                return GenCode(c.interior);
            }
            else if(context is SimpleOpContext) {
                var c = context as SimpleOpContext;
                return c.op + GenCode(c.a);
            }
            else if(context is OpContext) {
                var c = context as OpContext;
                if(c.op == "->") {
                    return $"Dependencies.To({GenCode(c.a)},{GenCode(c.b)})";
                }
                return GenCode(c.a) + " " + c.op + " " + GenCode(c.b);
            }
            return null;
        }
    }
}