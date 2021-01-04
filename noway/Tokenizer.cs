using System;
using System.Linq;
using System.Collections.Generic;

namespace Noway {
    public class Token {
        public static Token Blank = new Token() {line = -1, index = -1, value = ""};
        public int line;
        public int index;
        public string value;
        public override string ToString() {
            
            return $"line: {line}, index: {index}, value: {value}";
        }
    }
    public class Tokenizer {
        string text;
        int i = -1;
        public static List<char> breakChars = new List<char>() { '{', '}', '(', ')', ',', ':', ';', '.', '[', ']'};
        public static List<string> operators = new List<string>() { "->", "<=", ">=", "==", "*=", "/=", "%=", "+=", "-=", "!=", "*", "/", "+", "-", "%", "=", "<", ">", "^", "&", "|", "!"};
        public static List<char> ignoredBreakChars = new List<char>() { ' ', '\t', '\r', '\n'};
        public List<Token> tokens;

        private int line;
        private int index;

        public int currentIndex = -1;
        public int peekIndex = -1;

        public Tokenizer(string text) => this.text = text;
        public char peek() => i + 1 < text.Length ? text[i + 1] : (char)0;
        public string peek(int length) => i + length <= text.Length ? text.Substring(i, length) : "";
        public char next() {
            char c = peek();
            if(i >= 0 && text[i] == '\n') {
                line++;
                index = 0;
            } else index++;
            i++;
            return c;
        }
        public void Tokenize() {
            bool inQuotes = false;
            bool inSingleQuotes = false;
            tokens = new List<Token>();
            char c;
            string ex = "";
            string token = "";
            Action add = () => {
                if(token != "") tokens.Add(new Token() {line = line, index = index, value = token});
                token = "";
            };
            while((c = next()) != 0) {
                if(c == '"') inQuotes = !inQuotes;
                if(c == '\'') inSingleQuotes = !inSingleQuotes;
                if(!inQuotes && !inSingleQuotes) {
                    if(operators.Any(s => (ex = peek(s.Length)) == s)) {
                        i += ex.Length - 1;
                        add(); token = ex; add();
                    }
                    else if(breakChars.Contains(c)) {
                        
                        // if(c == ',' && !char.IsWhiteSpace(peek(2)[1]) && peek(2)[1] != '.') {
                        //     token += c;
                        // }
                        add();
                        token = c + "";
                        add();
                        
                    }
                    else if(ignoredBreakChars.Contains(c)) add();
                    else token += c;
                }
                else token += c;
            }
            add();
        }
        public Token peekNext() {
            if(peekIndex + 1 < tokens.Count) {
                peekIndex++;
                return tokens[peekIndex];
            }
            return Token.Blank;
        }
        public List<Token> peekNext(int number) {
            var ret = new List<Token>();
            Token next;
            while((next = peekNext()) != Token.Blank) {
                ret.Add(next);
            }
            return ret;
        }
        public void advance(int number) {
            currentIndex += number;
            peekIndex = currentIndex;
        }
    }
}