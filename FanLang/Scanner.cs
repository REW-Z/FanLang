﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace FanLang
{
    public class Scanner
    {
        //Token Patterns  
        public List<TokenPattern> keywords;
        public List<TokenPattern> operators;
        public List<TokenPattern> literals;
        public TokenPattern identifierPattern;
        public TokenPattern whitespace;


        //log  
        private static bool enableLog = false;

        public Scanner()
        {
            //词法分析器初始化    
            keywords = new List<TokenPattern>();

            keywords.Add(new TokenPattern("extern", "extern\\W", 1));

            keywords.Add(new TokenPattern("var", "var\\W", 1));
            keywords.Add(new TokenPattern("class", "class\\W", 1));
            keywords.Add(new TokenPattern("void", "void\\W", 1));
            keywords.Add(new TokenPattern("bool", "bool\\W", 1));
            keywords.Add(new TokenPattern("int", "int\\W", 1));
            keywords.Add(new TokenPattern("float", "float\\W", 1));
            keywords.Add(new TokenPattern("string", "string\\W", 1));

            keywords.Add(new TokenPattern(",", ",", 0));
            keywords.Add(new TokenPattern(";", ";[\\s|\\t|\\n]", 1));
            keywords.Add(new TokenPattern("new", "new\\W", 1));
            keywords.Add(new TokenPattern("while", "while\\W", 1));
            keywords.Add(new TokenPattern("for", "for\\W", 1));
            keywords.Add(new TokenPattern("if", "if\\W", 1));
            keywords.Add(new TokenPattern("else", "else\\W", 1));
            keywords.Add(new TokenPattern("break", "break\\W", 1));
            keywords.Add(new TokenPattern("return", "return\\W", 1));
            keywords.Add(new TokenPattern("this", "this\\W", 1));

            operators = new List<TokenPattern>();

            operators.Add(new TokenPattern("(", "\\("));
            operators.Add(new TokenPattern(")", "\\)"));
            operators.Add(new TokenPattern("[", "\\["));
            operators.Add(new TokenPattern("]", "\\]"));
            operators.Add(new TokenPattern("{", "\\{"));
            operators.Add(new TokenPattern("}", "\\}"));

            operators.Add(new TokenPattern("=", "=(?![=\\+])"));
            operators.Add(new TokenPattern("+=", "\\+="));
            operators.Add(new TokenPattern("-=", "-="));
            operators.Add(new TokenPattern("*=", "\\*="));
            operators.Add(new TokenPattern("/=", "/="));
            operators.Add(new TokenPattern("%=", "%="));

            operators.Add(new TokenPattern("--", "--"));
            operators.Add(new TokenPattern("++", "\\+\\+"));


            operators.Add(new TokenPattern("==", "=="));
            operators.Add(new TokenPattern("!=", "!="));
            operators.Add(new TokenPattern("<=", "<="));
            operators.Add(new TokenPattern(">=", ">="));
            operators.Add(new TokenPattern(">", ">[^=>]", 1));
            operators.Add(new TokenPattern("<", "<[^=<]", 1));
            operators.Add(new TokenPattern("+", "\\+[^=\\+]", 1));
            operators.Add(new TokenPattern("-", "-[^=-]", 1));
            operators.Add(new TokenPattern("*", "\\*[^=\\*]", 1));
            operators.Add(new TokenPattern("/", "/[^=/]", 1));
            operators.Add(new TokenPattern("%", "%[^=\\%]", 1));

            operators.Add(new TokenPattern("||", "\\|\\|"));
            operators.Add(new TokenPattern("&&", "\\&\\&"));

            operators.Add(new TokenPattern("!", "![^=]", 1));

            operators.Add(new TokenPattern(".", "\\.[\\w]", 1));

            literals = new List<TokenPattern>();

            literals.Add(new TokenPattern("LITBOOL", "(true|false)[^a-zA-Z]", 1));
            literals.Add(new TokenPattern("LITINT", "[0-9]+[^\\d\\.]", 1));
            literals.Add(new TokenPattern("LITFLOAT", "[0-9]+\\.[0-9]+[F|f]\\D", 1));
            literals.Add(new TokenPattern("LITSTRING", "\\\"[^\\\"]*\\\"[^\\\"]", 1));

            identifierPattern = new TokenPattern("ID", "[a-z|A-Z|_][a-z|A-Z|_|0-9]*\\W", 1);

            whitespace = new TokenPattern("space", "[\\n|\\s|\\t]+");
        }

        public List<string> GetTokenNames()
        {
            List<string> results = new List<string>();
            results.AddRange(keywords.Select(p => p.tokenName));
            results.AddRange(operators.Select(p => p.tokenName));
            results.AddRange(literals.Select(p => p.tokenName));
            results.Add(identifierPattern.tokenName);

            return results;
        }

        public static bool PatternMatch(string input, TokenPattern pattern)
        {
            bool ismatch = Regex.IsMatch(input, "^" + pattern.regularExpression + "$");

            return ismatch;
        }

        public List<Token> Scan(string input)
        {
            StringBuilder strb = new StringBuilder(input);
            strb.Append('\n');
            string source = strb.ToString();

            List<Token> tokens = new List<Token>();

            Log("source:\n" + source);

            //Pointers  
            int lexemBegin = 0;
            int forward = 0;


            //line  
            int currLine = 1;//当前行数  

            Action<int> MovePointer = (offset) =>
            {
                lexemBegin += offset;
                forward = lexemBegin + 1;
            };

            //Scan  
            while (lexemBegin != source.Length)
            {
                if (lexemBegin + (forward - lexemBegin) > source.Length)
                {
                    Log("发现越界：" + (lexemBegin + (forward - lexemBegin)));
                    Log("总长度：" + source.Length);
                }
                var seg = source.Substring(lexemBegin, forward - lexemBegin);

                Log("[" + seg + "]" + "(" + lexemBegin + "," + forward + ")");

                //WHITE SPACE  
                if (seg != "" && PatternMatch(seg, whitespace))
                {
                    Log("\n>>>>> white space. length:" + seg.Length + "\n\n");

                    //识别换行  
                    string spaceStr = seg.Substring(0, seg.Length - whitespace.back);
                    for (int i = 0; i < spaceStr.Length; ++i)
                    {
                        if(spaceStr[i] == '\n')
                        {
                            currLine++;
                        }
                    }

                    MovePointer(seg.Length - whitespace.back);
                    continue;
                }

                //KEYWORDS
                bool continueRead = false;
                foreach (var kw in keywords)
                {
                    if (PatternMatch(seg, kw))
                    {
                        string keyword = seg.Substring(0, seg.Length - kw.back);

                        Log("\n>>>>> keyword:" + keyword + "\n\n");

                        Token token = new Token(keyword, PatternType.Keyword, currLine);
                        tokens.Add(token);


                        MovePointer(seg.Length - kw.back);

                        continueRead = true;
                        break;
                    }
                }
                if (continueRead)
                {
                    continue;
                }


                //OPERATORS
                continueRead = false;
                foreach (var op in operators)
                {
                    if (PatternMatch(seg, op))
                    {
                        string opStr = seg.Substring(0, seg.Length - op.back);
                        Log("\n>>>>> operator:" + opStr + "  tokenname:" + op.tokenName + "\n\n");

                        Token token = new Token(opStr, PatternType.Operator, currLine);
                        tokens.Add(token);

                        MovePointer(seg.Length - op.back);

                        continueRead = true;
                        break;
                    }
                }
                if (continueRead)
                {
                    continue;
                }


                //LIT
                continueRead = false;
                foreach (var lit in literals)
                {
                    if (PatternMatch(seg, lit))
                    {
                        string litstr = seg.Substring(0, seg.Length - lit.back);
                        Log("\n>>>>> literal value:" + lit.tokenName + ":" + litstr + "\n\n");

                        Token token = new Token(lit.tokenName, PatternType.Number, currLine, litstr);
                        tokens.Add(token);

                        MovePointer(seg.Length - lit.back);

                        continueRead = true;
                        break;
                    }
                }
                if (continueRead)
                {
                    continue;
                }


                //ID  
                if (PatternMatch(seg, identifierPattern))
                {
                    string identifierName = seg.Substring(0, seg.Length - identifierPattern.back);
                    Log("\n>>>>> identifier:" + identifierName + "\n\n");

                    //int idx = globalSymbolTable.AddIdentifier(identifierName);//词法分析阶段最好不创建符号表条目（编译原理p53）  
                    Token token = new Token(identifierPattern.tokenName, PatternType.Id, currLine, identifierName);
                    tokens.Add(token);

                    MovePointer(seg.Length - identifierPattern.back);
                    continue;
                }



                forward++;
            }


            //DEBUG  
            {
                Log("Token列表：");
                foreach(var token in tokens)
                {
                    Log(token.ToString());
                }

                Compiler.Pause("词法单元扫描完毕，共" + tokens.Count + "个...");
            }

            return tokens;
        }


        private static void Log(object content)
        {
            if(!enableLog) return;
            Console.WriteLine("Scanner >>>" + content);
        }
    }
}
