using System;
using System.Linq;
using System.Collections.Generic;

namespace Noway {
    public class Parser {
        Tokenizer tokenizer;
        List<Token> tokens;
        ICompiler compiler;
        public enum BlockMode {
            brackets = 0
        }
        BlockMode blockMode;
        
        public Parser() {
        }
        
        public ProgramContext Parse(string text, ICompiler compiler, BlockMode blockMode) {
            this.compiler = compiler;
            this.blockMode = blockMode;
            tokenizer = new Tokenizer(text);
            tokenizer.Tokenize();
            tokens = tokenizer.tokens;
            foreach(var t in tokenizer.tokens) {
                Console.WriteLine(t);
            }
            var mfc = new FunctionContext();
            mfc.name = "main";

            var output = new BlockContext() {parent = mfc};
            TypedContext t2 = null;
            while((t2 = GetContext(mfc, 0, 0, 0)) != null) {
                output.body.Add(t2);
            }

            if(output is BlockContext) {
                mfc.body = output as BlockContext;
            } else {
                mfc.body = new BlockContext() {parent = mfc, body = {output}};
            }

            var mcc = new ClassContext();
            mcc.name = "init";
            mcc.functions.Add(mfc);
            mfc.parent = mcc;

            var pc = new ProgramContext();
            pc.name = "test";
            pc.classes.Add(mcc);
            mcc.parent = pc;
            pc.main = mfc;

            return pc;
        }
        private static List<string> prefixOperators = new List<string>() { "-", "!" };
        private static Dictionary<string, int> operators = new Dictionary<string, int>() {
            { "+", 2 }, { "-", 2 }, { "*", 3 }, { "/", 3}, { "->", 0 }, { "==", 0 }, { "<", 1 }, { ">", 1 }, { "!=", 0 }
        };
        private static List<KeyValuePair<string, string>> implicitConversions = new List<KeyValuePair<string, string>>() {
            KeyValuePair.Create(SysLib.IntType.name, SysLib.DoubleType.name),
            KeyValuePair.Create(SysLib.IntType.name, SysLib.FloatType.name),
            KeyValuePair.Create(SysLib.IntType.name, SysLib.LongType.name),
            KeyValuePair.Create(SysLib.CharType.name, SysLib.IntType.name),
        };

        private static int NoOps = 1, NoBlocks = 2, NoRecursion = 3;
        private TypedContext GetContext(IScopeOwner context, int opDepth, int callDepth, int flags) {
            tokenizer.advance(0);
            var s0 = tokenizer.peekNext().value;
            TypedContext ret = null;
            
            //number
            if(int.TryParse(s0, out int unused) &&
                tokenizer.peekNext().value == "." &&
                float.TryParse(tokenizer.peekNext().value, out float num)) {
                tokenizer.advance(2);
                s0 += "." + num;
            } else {
                tokenizer.advance(0);
                tokenizer.peekNext();
            }

            //parenthesis
            if(s0 == "") {
                tokenizer.advance(0);
                return null;
            }
            else if(s0 == "(") {
                if(tokenizer.peekNext().value == ")") {
                    ret = new ParenContext() {parent = context};
                    tokenizer.advance(1);
                } else {
                    tokenizer.advance(1);
                    var t1 = GetContext(context, 0, 0, NoBlocks);
                    var sn1 = tokenizer.peekNext().value;
                    if(sn1 == ")" && t1 is TypedContext)
                        ret = new ParenContext() {parent = context, interior = t1 as TypedContext};
                    else
                        throw new Exception("yeet");
                }

            }

            //blocks and array declaration
            else if(blockMode == BlockMode.brackets && s0 == "{") {
                tokenizer.advance(1);
                var cret = new BlockContext();
                while(tokenizer.peekNext().value != "}") {
                    var next = GetContext(context, opDepth, 0, flags);
                    if(next != null)
                        cret.body.Add(next);
                    else throw new Exception("hmm");
                }
                if(cret.body.Count == 1 && cret.body[0] is SeperatedContext) {
                    tokenizer.advance(0);
                    var aret = new ArrayContext() {
                        parent = context
                    };
                    aret.content = (cret.body[0] as SeperatedContext)?.children ?? new List<TypedContext>() { cret.body[0] };
                    aret.type = $"{SysLib.ArrayType.name}<{aret.content[0].type}>";
                    ret = aret;
                } else {
                    tokenizer.advance(0);
                    ret = cret;
                }
            }

            //list declaration
            else if(s0 == "[") {
                tokenizer.advance(1);
                var t2 = GetContext(context, opDepth, callDepth+1, flags);
                if(tokenizer.peekNext().value != "]") throw new Exception("Expected one of these: ]");
                var cret = new ListContext();
                cret.parent = context;
                cret.content = (t2 as SeperatedContext)?.children ?? new List<TypedContext>() { t2 };
                cret.type = $"{SysLib.ListType.name}<{cret.content[0].type}>";
                ret = cret;
            }

            //for, while, foreach
            else if(s0 == "for") {
                tokenizer.advance(1);
                var cret = new LoopContext() {parent = context};
                var t2 = GetContext(cret, 0, 0, NoBlocks) as ParenContext;
                if(t2 == null) throw new Exception("expected a condition for the for loop");
                cret.condition = t2.interior;
                var t3 = GetContext(cret, 0, 0, 0);
                cret.body = t3;
                tokenizer.advance(-1);
                ret = cret;
            }
            else if(s0 == "if") {
                tokenizer.advance(1);
                var cret = new IfContext() {parent = context};
                var t2 = GetContext(cret, 0, 0, 0) as ParenContext;
                if(t2 == null || t2.type != SysLib.BoolType.name) throw new Exception("expected a boolean condition for the if statement");
                cret.condition = t2.interior;
                var t3 = GetContext(cret, 0, 0, 0);
                cret.body = t3;
                
                // while(tokenizer.peekNext().value == "elif") {

                // }
                if(tokenizer.peekNext().value == "else") {
                    tokenizer.advance(1);
                    var eret = new ElseContext() {parent = context, chainParent = cret};
                    eret.chainParent = cret;
                    var t4 = GetContext(eret, 0, 0, 0);
                    eret.body = t4;
                    ret = eret;
                } else {
                    ret = cret;
                }
                tokenizer.advance(-1);
            }
            else if(s0 == "break") {
                var cret = new BreakContext();
                cret.parent = context;
                cret.type = SysLib.VoidType.name;
                ret = cret;
            }

            //constants
            else if(int.TryParse(s0, out int i0))
                ret = new ConstContext() {value = i0, type = SysLib.IntType.name};
            else if(double.TryParse(s0, out double d0))
                ret = new ConstContext() {value = d0, type = SysLib.DoubleType.name};
            else if(float.TryParse(s0, out float f0))
                ret = new ConstContext() {value = f0, type = SysLib.FloatType.name};
            else if(long.TryParse(s0, out long l0))
                ret = new ConstContext() {value = l0, type = SysLib.LongType.name};
            else if(bool.TryParse(s0, out bool b0))
                ret = new ConstContext() {value = b0, type = SysLib.BoolType.name};
            else if(s0[0] == '"' && s0[s0.Length - 1] == '"')
                ret = new ConstContext() {value = s0.Substring(1, s0.Length - 2), type = SysLib.StringType.name};
            else if(s0.Length == 3 && s0[0] == '\'' && s0[2] == '\'')
                ret = new ConstContext() {value = s0[1], type = SysLib.CharType.name};

            //prefix operators
            else if(prefixOperators.Contains(s0)) {
                tokenizer.advance(1);
                var t2 = GetContext(context, opDepth, callDepth+1, NoOps);
                ret = new SimpleOpContext() {a = t2, op = s0};
                tokenizer.advance(-1);
            }

            //variables, functions, etc.
            else {
                if(ResolveVariable(context, s0, out var match))
                    ret = new VarContext() {variable = match};
                else {
                    if(tokenizer.peekNext().value == ".") {
                        //check imported types

                        
                        //namespace prefixed types
                        string typeName = s0;

                        //reset tokenizer for while loop
                        tokenizer.advance(0);
                        tokenizer.peekNext();

                        int steps = 0;
                        TypeData td = null;
                        while(!compiler.ValidateType(typeName, out td)) {
                            if(tokenizer.peekNext().value != ".") break;
                            typeName += '.' + tokenizer.peekNext().value;
                            steps += 2;
                        }
                        if(td != null) {
                            ret = new StaticTypeContext() { parent = context, type = typeName };
                            tokenizer.advance(steps);

                        } else {
                            tokenizer.advance(0);
                        }
                    } else {
                        var vc = new VarDecContext() {name = s0};
                        context.variables.Add(vc);
                        ret = vc;
                    }
                }
            }
            tokenizer.advance(1);
            
            ret.parent = context;
            //all operators
            var s1 = tokenizer.peekNext().value;

            if(flags == NoRecursion) {
                tokenizer.advance(0);
                return ret;
            }

            //indexers
            if((ret is VarContext || ret is MemberContext) && s1 == "[") {
                tokenizer.advance(1);
                var t2 = GetContext(context, 0, callDepth+1, 0);
                
                //get arguments  
                var arguments = t2 is SeperatedContext ? (t2 as SeperatedContext).children : new List<TypedContext>() { t2 };
                if(tokenizer.peekNext().value != "]")
                    throw new Exception("expected one of these: ]");
                
                if(tokenizer.peekNext().value == "=") {
                    tokenizer.advance(2);
                    var t3 = GetContext(context, 0, callDepth+1, 0);
                    arguments.Add(t3);
                    var cret = new CallContext() {
                        owner = ret,
                        methodName = "_setIndex",
                        parent = context,
                        arguments = arguments
                    };
                    if(compiler.ValidateCall(cret, out MethodData md)) {
                        cret.type = md.returnType;
                        ret = cret;
                    } else throw new Exception("no indexer for this boi");
                } else {               
                    var cret = new CallContext() {
                        owner = ret,
                        methodName = "_getIndex",
                        parent = context,
                        arguments = arguments
                    };
                    if(compiler.ValidateCall(cret, out MethodData md)) {
                        cret.type = md.returnType;
                        ret = cret;
                    } else throw new Exception("no indexer for this boi");
                    tokenizer.advance(1);
                    s1 = tokenizer.peekNext().value;
                }
            }

            //array declaration
            else if(ret is StaticTypeContext && s1 == "[") {
                tokenizer.advance(1);
                if(tokenizer.peekNext().value == "]") {
                    ret = new ListContext() {
                        type = $"{SysLib.ListType.name}<{ret.type}>",
                        parent = context
                    };
                } else {
                    var t2 = GetContext(context, 0, callDepth+1, 0);
                    if(t2.type != SysLib.IntType.name) throw new Exception("array initializer must have an integer length");
                    if(tokenizer.peekNext().value != "]")
                        throw new Exception("expected one of these: ]");
                        
                    ret = new ArrayInitContext() {
                        type = $"{SysLib.ArrayType.name}<{ret.type}>",
                        length = t2,
                        parent = context
                    };
                }
                tokenizer.advance(1);          
            }
                

            //calls and member accessing, 
            if(s1 == ".") {
                while(s1 == ".") {
                    var type = ret.type;
                    var memberOrMethod = tokenizer.peekNext().value;
                    if(tokenizer.peekNext().value == "(") {
                        //method
                        tokenizer.advance(2);
                        var t2 = GetContext(context, 0, callDepth+1, NoRecursion) as ParenContext;
                        if(t2 == null) throw new Exception("expected arguments");

                        //resolve arguments
                        List<TypedContext> arguments = new List<TypedContext>();
                        if(t2.interior != null) {
                            if(t2.interior is SeperatedContext)
                                arguments = ((SeperatedContext)t2.interior).children;
                            else 
                                arguments.Add(t2);
                        }
                        var cret = new CallContext() {
                                parent = context,
                                owner = ret,
                                methodName = memberOrMethod,
                                arguments = arguments
                            };
                        if(compiler.ValidateCall(cret, out MethodData md)) {
                            cret.type = md.returnType;
                            ret = cret;
                        } else throw new Exception("could not find method");
                        tokenizer.advance(0);
                        //if(tokenizer.peekNext().value != ")") throw new Exception("Expected one of these: )");
                    } else {
                        //member
                        var cret = new MemberContext() {
                            parent = context,
                            owner = ret,
                            memberName = memberOrMethod,
                        };
                        if(compiler.ValidateMember(cret, out MemberData md)) {
                            tokenizer.advance(2);
                            cret.type = md.returnType;
                            ret = cret;
                        } else throw new Exception("could not find member");
                    }
                    s1 = tokenizer.peekNext().value;
                }
                //tokenizer.advance(0);
            }

            //Parameter lists
            else if(s1 == "," && ret is VarDecContext) {
                var cret = new ParamContext();
                var prev = ret as VarDecContext;
                cret.children.Add(ret);
                cret.parent = context;
                while(s1 == ",") {
                    tokenizer.advance(1);
                    var t2 = GetContext(context, 0, callDepth+1, NoRecursion) as VarDecContext;
                    if(t2 == null) throw new Exception("excepted parameter");
                    if(t2.type == null) t2.type = prev.type;
                    prev = t2;
                    cret.children.Add(t2);
                    s1 = tokenizer.peekNext().value;
                }
                ret = cret;
            }

            //list definition
            else if(s1 == "," && flags != 4) {
                var cret = new SeperatedContext();
                cret.children.Add(ret);
                cret.parent = context;
                while(s1 == ",") {
                    tokenizer.advance(1);
                    var t2 = GetContext(context, 0, callDepth+1, NoRecursion);
                    cret.children.Add(t2);
                    s1 = tokenizer.peekNext().value;

                    //find common type
                    cret.type = t2.type;
                }
                ret = cret;
            }

            //assignments
            if(s1 == "=" && flags != NoOps) {
                TypedContext variable = ret as VarDecContext ?? (ret as VarContext)?.variable;
                if(variable == null) {
                    if(ret is MemberContext)
                        variable = ret;
                    else throw new Exception("you can only assign variables");
                }

                tokenizer.advance(1);
                var t2 = GetContext(context, opDepth, callDepth+1, flags);
                variable.type = t2.type;
                ret = new AssignContext() {parent = context, variable = ret, value = t2};
            }

            //from
            else if(s1 == ":") {
                var variable = ret as VarDecContext ?? (ret as VarContext).variable;
                if(variable == null) throw new Exception("you must get a variable from the following expression");
                tokenizer.advance(1);
                var t2 = GetContext(context, opDepth, callDepth+1, flags);
                var call = new CallContext() {
                    owner = t2,
                    methodName = "_getIter",
                    parent = context
                };
                if(compiler.ValidateCall(call, out MethodData md)) {
                    if(variable.type == null) {
                        variable.type = md.returnType.Between("<", ">");
                    }
                } else throw new Exception("Cannot iterate a type without a _getIter method");
                ret = new FromContext() { parent = context, reference = variable, body = t2 };
            }

            //operators
            else if(operators.ContainsKey(s1) && flags != NoOps) {
                tokenizer.advance(1);
                var t2 = GetContext(context, opDepth+1, callDepth+1, flags);

                var cret = new OpContext() {a = ret, op = s1, b = t2};    
                if(t2 is OpContext) {
                    int p0 = operators[s1];
                    int p1 = operators[((OpContext)t2).op];
                    if(p0 >= p1) {
                        OpContext deepA = (OpContext)t2;
                        for(; deepA.a is OpContext; deepA = (OpContext)(deepA.a));
                        cret.b = deepA.a;
                        deepA.a = cret;
                        cret = (OpContext)t2;
                    }
                }
                if(opDepth == 0) {
                    //resolve type
                    ResolveType(cret);
                }
                ret = cret;
            } else tokenizer.advance(0);

            ret.parent = context;
            
            return ret;
        }

        private void ResolveType(TypedContext context) {
            if(context is ParenContext) {
                ResolveType((context as ParenContext).interior);
            }
            else if(context is SimpleOpContext) {
                ResolveType((context as SimpleOpContext).a);
            }
            else if(context is OpContext) {
                var opContext = context as OpContext;
                ResolveType(opContext.a);
                ResolveType(opContext.b);
                if(opContext.a.type == opContext.b.type) {
                    opContext.type = opContext.a.type;
                } else {
                    bool found = false;
                    
                    foreach(var v in implicitConversions) {
                        
                        if(v.Key == opContext.a.type && v.Value == opContext.b.type) {
                            opContext.a = new ConvertContext() {
                                interior = opContext.a,
                                type = v.Value,
                                parent = opContext.parent
                            };
                            opContext.type = v.Value;
                            found = true;
                            break;
                        } else if(v.Key == opContext.b.type && v.Value == opContext.a.type) {
                            opContext.b = new ConvertContext() {
                                interior = opContext.b,
                                type = v.Value,
                                parent = opContext.parent
                            };
                            opContext.type = v.Value;
                            found = true;
                            break;
                        }
                    }
                    if(!found) {
                        if(opContext.a.type == SysLib.StringType.name) {
                            opContext.b = new ConvertContext() {
                                interior = opContext.b,
                                type = SysLib.StringType.name,
                                parent = opContext.parent
                            };
                            opContext.type = SysLib.StringType.name;
                        }
                        else if(opContext.b.type == SysLib.StringType.name) {
                            opContext.a = new ConvertContext() {
                                interior = opContext.a,
                                type = SysLib.StringType.name,
                                parent = opContext.parent
                            };
                            opContext.type = SysLib.StringType.name;
                        }
                        else
                            throw new Exception("type not resolved");
                    }
                }
                if(opContext.op == "==" || opContext.op == "<" || opContext.op == ">" || opContext.op == "!=") {
                    opContext.type = SysLib.BoolType.name;
                }
                else if(opContext.op == "->") {
                    opContext.type = $"{SysLib.IterableType.name}<{opContext.type}>";
                }
            }
        }
        private bool GetGenericInfo(string fullname, out string name, out string[] args) {
            name = null;
            args = null;
            int index = fullname.IndexOf('<');
            if(index == -1) return false;
            args = fullname.Substring(index + 1, fullname.LastIndexOf('>') - index - 1).Replace(" ", "").Split(',');
            name = fullname.Substring(0, index);
            return true;
        }
        private bool ResolveVariable(IScopeOwner scope, string name, out VarDecContext variable) {
            variable = scope.variables.FirstOrDefault(v => v.name == name);
            if(variable != null) return true;
            if(scope.parent != null) return ResolveVariable(scope.parent, name, out variable);
            return false;
        }
    }
}