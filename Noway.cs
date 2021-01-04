using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Noway {
    public struct Hmm<T> {

    }
    public class Program {
        public static void Main(string[] args) {
            var c = new CSharpCompiler();
            var n = new NowayCompiler(c);

            // SysLib.Initialize();
            
            string all = File.ReadAllText(@"C:\Users\Justin\Documents\Programming\Noway\out\source.nw");
            var p = new Parser();

            var program = p.Parse(all, n, Parser.BlockMode.brackets);
            c.Compile(program);
        }
    }
}