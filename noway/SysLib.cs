using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections;

namespace Noway {
    public class SysLib {

        public static TypeData AnyType = new TypeData() { name = "sys.any" };

        public static TypeData BoolType = new TypeData() { name = "sys.bool", parent = AnyType };
        public static TypeData ByteType = new TypeData() { name = "sys.byte", parent = AnyType };
        public static TypeData CharType = new TypeData() { name = "sys.char", parent = AnyType };
        public static TypeData DoubleType = new TypeData() { name = "sys.double", parent = AnyType };
        public static TypeData FloatType = new TypeData() { name = "sys.float", parent = AnyType };
        public static TypeData IntType = new TypeData() { name = "sys.int", parent = AnyType };
        public static TypeData LongType = new TypeData() { name = "sys.long", parent = AnyType };
        public static TypeData ShortType = new TypeData() { name = "sys.short", parent = AnyType };
        public static TypeData VoidType = new TypeData() { name = "sys.void", parent = VoidType };
        public static TypeData ObjectType = new TypeData() { name = "sys.obj", parent = AnyType };
        public static TypeData ConsoleType = new TypeData() {
            name = "sys.console", parent = ObjectType,
            methods = {
                new MethodData() {
                    name = "print",
                    parameterNames = { "str" },
                    parameterTypes = { "sys.string"},
                    returnType = "sys.void"
                },
                new MethodData() {
                    name = "println",
                    parameterNames = { "str" },
                    parameterTypes = { "sys.string"},
                    returnType = "sys.void"
                },
                new MethodData() {
                    name = "println",
                    returnType = "sys.void"
                },
                new MethodData() {
                    name = "readln",
                    returnType = "sys.string"
                },
                new MethodData() {
                    name = "wait",
                    returnType = "sys.void"
                },
                new MethodData() {
                    name = "clear",
                    returnType = "sys.void"
                }
            }
        };
        public static TypeData ArrayType = new TypeData() {
            name = "sys.arr", parent = IterableType,
            isGeneric = true,
            genericArgs = { "T" },
            methods = {
                new MethodData() {
                    name = "_getIndex",
                    parameterNames = { "index" },
                    parameterTypes = { "sys.int" },
                    returnType = "T"
                },
                new MethodData() {
                    name = "_setIndex",
                    parameterNames = { "index", "value" },
                    parameterTypes = { "sys.int", "T" },
                    returnType = "sys.void"
                },
                new MethodData() {
                    name = "_getIter",
                    returnType = "sys.iterator<T>"
                },
                new MethodData() {
                    name = "len",
                    returnType = "sys.int"
                }
            }
        };
        public static TypeData RandomType = new TypeData() {
            name = "sys.rand", parent = ObjectType,
            methods = {
                new MethodData() {
                    name = "nextInt",
                    parameterNames = { "min", "max" },
                    parameterTypes = { "sys.int", "sys.int" },
                    returnType = "sys.int"
                }
            }
        };
        public static TypeData ListType = new TypeData() {
            name = "sys.list", parent = IterableType,
            isGeneric = true,
            genericArgs = { "T" },
            methods = {
                new MethodData() {
                    name = "_getIndex",
                    parameterNames = { "index" },
                    parameterTypes = { "sys.int" },
                    returnType = "T"
                },
                new MethodData() {
                    name = "_setIndex",
                    parameterNames = { "index", "value" },
                    parameterTypes = { "sys.int", "T" },
                    returnType = "sys.void"
                },
                new MethodData() {
                    name = "add",
                    parameterNames = { "value" },
                    parameterTypes = { "T" },
                    returnType = "sys.void"
                },
                new MethodData() {
                    name = "remove",
                    parameterNames = { "value" },
                    parameterTypes = { "T" },
                    returnType = "sys.void"
                },
                new MethodData() {
                    name = "_getIter",
                    returnType = "sys.iterator<T>"
                },
                new MethodData() {
                    name = "len",
                    returnType = "sys.int"
                }
            }
        };
        public static TypeData IterableType = new TypeData() {
            name = "sys.iterable", parent = ObjectType,
            isGeneric = true,
            genericArgs = { "T" },
            methods = {
                new MethodData() {
                    name = "_getIter",
                    returnType = "sys.iterator<T>"
                }
            }
        };
        public static TypeData IteratorType = new TypeData() {
            name = "sys.iterator", parent = ObjectType,
            isGeneric = true,
            genericArgs = { "T" },
            methods = {
                new MethodData() {
                    name = "moveNext",
                    returnType = "sys.bool"
                },
                new MethodData() {
                    name = "getCurrent",
                    returnType = "T"
                },
                new MethodData() {
                    name = "reset",
                    returnType = "sys.void"
                }
            }
        };
        public static TypeData StringType = new TypeData() {
            name = "sys.string", parent = ObjectType,
            methods = {
                new MethodData() {
                    name = "upper",
                    returnType = "sys.string"
                },
                new MethodData() {
                    name = "lower",
                    returnType = "sys.string"
                },
                new MethodData() {
                    name = "sub",
                    parameterNames = { "start", "length" },
                    parameterTypes = { "sys.int", "sys.int" },
                    returnType = "sys.string"
                },
                new MethodData() {
                    name = "_getIter",
                    returnType = "sys.iterator<sys.char>"
                },
                new MethodData() {
                    name = "_getIndex",
                    parameterNames = { "index" },
                    parameterTypes = { "sys.int" },
                    returnType = "sys.char"
                },
                new MethodData() {
                    name = "split",
                    parameterNames = { "spliter" },
                    parameterTypes = { "sys.char" },
                    returnType = "sys.arr<sys.string>"
                },
                new MethodData() {
                    name = "replace",
                    parameterNames = { "find", "replace" },
                    parameterTypes = { "sys.string", "sys.string" },
                    returnType = "sys.string"
                }
            }
        };
    }
}