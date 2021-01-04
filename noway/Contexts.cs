using System.Collections.Generic;
using System.Linq;

namespace Noway {
    public interface IContext {
        IScopeOwner parent { get; set; }
    }
    public interface IScopeOwner : IContext {
        List<VarDecContext> variables { get; set; }
    }
    public interface BodyContext : IContext {}
    public interface TypedContext : BodyContext {
        string type { get; set; }
    }
    public class ProgramContext : IScopeOwner {
        public string name;
        public IScopeOwner parent { get; set; }
        public List<ClassContext> classes = new List<ClassContext>();
        public List<VarDecContext> variables { get; set; } = new List<VarDecContext>();
        public FunctionContext main;
    }
    public class ClassContext : IScopeOwner {
        public string name;
        public IScopeOwner parent { get; set; }
        public List<FunctionContext> functions = new List<FunctionContext>();
        public List<VarDecContext> variables { get; set; } = new List<VarDecContext>();
    }
    public interface ICallContext : TypedContext {
        string methodName { get; set; }
        List<TypedContext> arguments { get; set; }
    }
    public class InstanceCallContext : ICallContext {
        public string type { get; set; }
        public string baseType { get; set; }
        public string methodName { get; set; }
        public IScopeOwner parent { get; set; }
        public List<TypedContext> arguments { get; set; } = new List<TypedContext>();
    }
    public class CallContext : ICallContext {
        public string type { get; set; }
        public TypedContext owner;
        public int flags;
        public string methodName { get; set; }
        public IScopeOwner parent { get; set; }
        public List<TypedContext> arguments { get; set; } = new List<TypedContext>();
    }
    public class ArrayContext : TypedContext {
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
        public List<TypedContext> content { get; set; } = new List<TypedContext>();
    }
    public class ListContext : TypedContext {
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
        public List<TypedContext> content { get; set; } = new List<TypedContext>();
    }
    /*
    public interface IMemberContext : TypedContext {
        string baseType { get; set; }
        string memberName { get; set; }
    }*/
    public class MemberContext : TypedContext {
        public string type { get; set; }
        public TypedContext owner;
        public string memberName;
        public IScopeOwner parent { get; set; }
    }
    public class FunctionContext : IScopeOwner, TypedContext {
        public string name;
        public IScopeOwner parent { get; set; }
        public BlockContext body;
        public List<VarDecContext> variables { get; set; } = new List<VarDecContext>();
        public string type { get => body.body.LastOrDefault()?.type ?? SysLib.VoidType.name; set {} }
    }
    public class BlockContext : TypedContext {
        public IScopeOwner parent { get; set; }
        public List<TypedContext> body = new List<TypedContext>();
        //public List<VarDecContext> variables { get; set; } = new List<VarDecContext>();
        public string type {
            get => body.LastOrDefault()?.type ?? SysLib.VoidType.name;
            set {}
        }
    }
    public class VarDecContext : TypedContext {
        public string name;
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
        public override string ToString() => "dec " + name;
    }
    public class VarContext : TypedContext {
        public VarDecContext variable;
        public IScopeOwner parent { get; set; }
        public string type {
            get => variable.type;
            set {}
        }
        public override string ToString() => variable.name;
    }
    public class ParamContext : TypedContext {
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
        public List<TypedContext> children = new List<TypedContext>();
    }
    public class SeperatedContext : TypedContext {
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
        public List<TypedContext> children = new List<TypedContext>();
    }
    public class LoopContext : IScopeOwner, TypedContext {
        public string type { get => SysLib.VoidType.name; set {} }
        public IScopeOwner parent { get; set; }
        public List<VarDecContext> variables { get; set; } = new List<VarDecContext>();
        public TypedContext condition;
        public TypedContext body;
    }
    public class BreakContext : TypedContext {
        public string type { get => SysLib.VoidType.name; set {} }
        public IScopeOwner parent { get; set; }
    }
    public class ReturnContext : TypedContext {
        public string type { get => body.type; set {}}
        public IScopeOwner parent { get; set; }
        public TypedContext body;
    }
    public class FromContext : TypedContext {
        public string type { get => reference.type; set {}}
        public IScopeOwner parent { get; set; }
        public VarDecContext reference;
        public TypedContext body;
    }
    public class SimpleOpContext : TypedContext {
        public TypedContext a;
        public string op;
        public string type {
            get => a.type;
            set {}
        }
        public IScopeOwner parent { get; set; }
    }
    public class OpContext : TypedContext {
        public TypedContext a;
        public string op;
        public TypedContext b;
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
        public override string ToString()
        {
            return $"({a} {op} {b})";
        }
    }
    public class ConvertContext : TypedContext {
        public TypedContext interior;
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
    }
    public class AssignContext : TypedContext {
        public TypedContext variable;
        public TypedContext value;
        public IScopeOwner parent { get; set; }
        public string type { get => SysLib.VoidType.name; set {} }
    }
    public class ConstContext : TypedContext {
        public object value;
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
        public override string ToString() => "c" + value;
    }
    public class StaticTypeContext : TypedContext {
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
    }
    public class ParenContext : TypedContext {
        public TypedContext interior;
        public string type {
            get => interior.type;
            set {}
        }
        public IScopeOwner parent { get; set; }
        public override string ToString() => $"({interior})";
    }
    public class ArrayInitContext : TypedContext {
        public TypedContext length;
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
    }
    public class IfContext : TypedContext, IScopeOwner {
        public List<VarDecContext> variables { get; set; } = new List<VarDecContext>();
        public TypedContext condition;
        public TypedContext body;
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
    }
    public class OrifContext : TypedContext, IScopeOwner {
        public List<VarDecContext> variables { get; set; } = new List<VarDecContext>();
        public TypedContext chainParent;
        public TypedContext condition;
        public TypedContext body;
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
    }
    public class ElseContext : TypedContext, IScopeOwner {
        public List<VarDecContext> variables { get; set; } = new List<VarDecContext>();
        public TypedContext chainParent;
        public TypedContext body;
        public string type { get; set; }
        public IScopeOwner parent { get; set; }
    }
}