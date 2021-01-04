using System;
using System.Linq;
using System.Collections.Generic;

namespace Noway {
    public interface ICompiler {
        void Compile(ProgramContext program);
        bool ValidateType(string name, out TypeData type);
        bool ValidateCall(CallContext call, out MethodData type);
        bool ValidateMember(MemberContext member, out MemberData type);
    }
    public enum Flags {
        SysLib = 1,
        Generic = 2,
        GenericArg = 4,
        Public = 8,
        Private = 16,
        Static = 32
    }
    public class NowayCompiler : ICompiler {
        public List<TypeData> knownTypes = new List<TypeData>();
        public List<ICompiler> compilers = new List<ICompiler>();

        public NowayCompiler(params ICompiler[] supportedLanguages) {
            if(knownTypes.Count == 0) {
                knownTypes.AddRange(
                    typeof(SysLib).GetFields().Where(
                        f => f.IsStatic && f.IsPublic && f.FieldType == typeof(TypeData)
                    ).Select(
                        f => (TypeData)f.GetValue(null)
                    )
                );
            }
            compilers.AddRange(supportedLanguages);
        }

        public void Compile(ProgramContext program) {
            
        }

        private int stackCounter;
        private TypeData CreateGenericType(TypeData genericType, List<TypeData> args) {
            stackCounter++;
            if(genericType.genericArgs.Count != args.Count) throw new Exception("bruh");
            var ret = new TypeData();
            ret.name = genericType.name + '<' + args.Select(t => t.name).AddCommas().Replace(" ", "") + '>';
            ret.isGeneric = true;
            ret.parent = genericType;
            ret.genericArgs = args.Select(a => a.name).ToList();
            Func<string, string> repl = null;
            repl = s => {
                int index = genericType.genericArgs.IndexOf(s);
                if(index >= 0) return ret.genericArgs[index];

                if(!ValidateType(s, out TypeData t)) throw new Exception("type no exist");

                if(t.isGeneric) {
                    var args = t.genericArgs.Select(a => repl(a));
                    return $"{t.parent.name}<{args.AddCommas()}>";
                }
                return t.name;
            };
            foreach(var m in genericType.members) {
                ret.members.Add(new MemberData() {
                    name = m.name,
                    documentation = m.documentation,
                    returnType = repl(m.returnType)
                });
            }
            foreach(var m in genericType.methods) {
                ret.methods.Add(new MethodData() {
                    name = m.name,
                    documentation = m.documentation,
                    parameterNames = m.parameterNames.ToList(),
                    parameterTypes = m.parameterTypes.Select(p => repl(p)).ToList(),
                    returnType = repl(m.returnType)
                });
            }
            stackCounter--;
            if(stackCounter == 0)
                knownTypes.Add(ret);
            return ret;
        }
        private TypeData GetTypeByName(string name) {
            var ret = knownTypes.FirstOrDefault(t => t.name == name);
            if(ret == null) {
                foreach(var c in compilers) {
                    if(c.ValidateType(name, out ret)) {
                        ret.language = c;
                        knownTypes.Add(ret);
                        return ret;
                    }
                }
            }
            return ret;
        }
        public bool ValidateType(string fullName, out TypeData t) {
            fullName = fullName.Replace(" ", "");
            t = null;
            if(fullName.Length > 2 && fullName.LastIndexOf("[]") == fullName.Length - 2) {
                return ValidateType($"{SysLib.ArrayType.name}<{fullName.Substring(0, fullName.Length - 2)}>", out t);
            }

            var index = fullName.IndexOf('<');
            
            if(index >= 0) {
                var genType = GetTypeByName(fullName.Substring(0, index));
                if(genType == null || !genType.isGeneric)
                    return false;

                var args = fullName.Substring(index + 1, fullName.LastIndexOf('>') - index - 1).Replace(" ", "").Split(',');
                
                var argData = new List<TypeData>();
                foreach(var arg in args) {
                    TypeData argType;
                    if(!arg.Contains('.')) {
                        argType = new TypeData() {
                            name = arg,
                            parent = SysLib.AnyType,
                            isGenericArg = true
                        };
                    }
                    else if(!ValidateType(arg, out argType)) return false;
                    argData.Add(argType);
                }
                t = CreateGenericType(genType, argData);
                return true;
            }

            t = GetTypeByName(fullName);
            return t != null;
        }
        public bool ValidateCall(CallContext callContext, out MethodData method) {
            method = null;
            if(!ValidateType(callContext.owner.type, out TypeData callBase)) return false;
            method = callBase.methods.FirstOrDefault(m => m.name == callContext.methodName && m.parameterTypes.SequenceEqual(callContext.arguments.Select(a => a.type))); 
            if(method == null && callBase.language != null) {
                if(callBase.language.ValidateCall(callContext, out method)) {
                    callBase.methods.Add(method);
                    return true;
                }
                return false;
            }
            return method != null;
        }
        public bool ValidateMember(MemberContext memberContext, out MemberData member) {
            member = null;
            if(!ValidateType(memberContext.owner.type, out TypeData memberBase)) return false;
            member = memberBase.members.FirstOrDefault(m => m.name == memberContext.memberName);
            if(member == null && memberBase.language != null) {
                if(memberBase.language.ValidateMember(memberContext, out member)) {
                    memberBase.members.Add(member);
                    return true;
                }
            }
            return member != null;
        }
    }
    public class TypeData {
        public Flags flags;
        public TypeData parent;
        public ICompiler language;
        public string name;
        public bool isGeneric;
        public bool isGenericArg;
        public List<string> genericArgs = new List<string>();
        public List<MemberData> members = new List<MemberData>();
        public List<MethodData> methods = new List<MethodData>();
    }
    public class MemberData {
        public Flags flags;
        public string name;
        public string returnType;
        public string documentation;
    }
    public class MethodData {
        public Flags flags;
        public string name;
        public List<string> parameterNames = new List<string>();
        public List<string> parameterTypes = new List<string>();
        public string returnType;
        public string documentation;
    }
}