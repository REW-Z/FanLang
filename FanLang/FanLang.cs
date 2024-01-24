using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;



namespace FanLang
{
    /// <summary>
    /// 模式  
    /// </summary>
    public enum PatternType
    {
        Keyword,
        Operator,
        Id,
        Number,
    }



    /// <summary>
    /// 词法单元  
    /// </summary>
    public struct Token
    {
        /// <summary>
        /// 词法单元名  
        /// </summary>
        public string name;

        /// <summary>
        /// 类型  
        /// </summary>
        public PatternType patternType;

        /// <summary>
        /// 属性值（数字number的属性值通常是具体的词素 / 标识符id的属性值通常是指向符号表条目的指针）  
        /// </summary>
        public string attribute;


        public Token(string name, PatternType type, string attribute = null)
        {
            this.name = name;
            this.patternType = type;
            this.attribute = attribute;
        }
        public override string ToString()
        {
            return "<" + name + (string.IsNullOrEmpty(attribute) ? "" : ("," + attribute)) + ">";
        }
    }


    /// <summary>
    /// 模式  
    /// </summary>
    public class Pattern
    {
        public string regularExpression;

        public int back;

        public Pattern(string regularExpr, int back = 0)
        {
            this.regularExpression = regularExpr;
            this.back = back;
        }
    }

    /// <summary>
    /// 词素  
    /// </summary>
    //public struct Lexeme
    //{
    //}

    /// <summary>
    /// 作用域  
    /// </summary>
    public class Scope
    {
        public SymbolTable symbolTable = new SymbolTable();

        public List<Scope> subScopes = new List<Scope>();
    }
    /// <summary>
    /// 符号表  
    /// </summary>
    public class SymbolTable
    {
        public List<string> symbols = new List<string>();

        public int AddSymbol(string symbol)
        {
            symbols.Add(symbol);
            return symbols.Count - 1;
        }
    }




    /// <summary>
    /// 编译器  
    /// </summary>
    public class Compiler
    {
        //Token Patterns  
        public List<Pattern> keywords;
        public List<Pattern> operators;
        public Pattern whitespace;
        public Pattern identifierPattern;
        public Pattern numberPattern;


        //Symbol Tables
        public Scope globalScope;


        //CTOR  
        public Compiler()
        {
            keywords = new List<Pattern>();

            keywords.Add(new Pattern("var\\s", 1));
            keywords.Add(new Pattern(";[\\s|\\t|\\n]", 1));
            keywords.Add(new Pattern("if\\W", 1));
            keywords.Add(new Pattern("else\\W", 1));

            operators = new List<Pattern>();
            operators.Add(new Pattern("=="));
            operators.Add(new Pattern("<="));
            operators.Add(new Pattern(">="));
            operators.Add(new Pattern(">"));
            operators.Add(new Pattern("<"));
            operators.Add(new Pattern("="));
            operators.Add(new Pattern("\\+"));
            operators.Add(new Pattern("-"));
            operators.Add(new Pattern("\\*"));
            operators.Add(new Pattern("/"));
            operators.Add(new Pattern("%"));


            whitespace = new Pattern("[\\n|\\s|\\t]+");

            identifierPattern = new Pattern("[a-z|A-Z][a-z|A-Z|0-9]*\\W", 1);

            numberPattern = new Pattern("[0-9]+\\D", 1);

            globalScope = new Scope();
        }

        //模式匹配
        private static bool PatternMatch(string input, Pattern pattern)
        {
            bool ismatch = Regex.IsMatch(input, "^" + pattern.regularExpression + "$");

            return ismatch;
        }



        /// <summary>
        /// 此法单元扫描
        /// </summary>
        public List<Token> Scan(string source)
        {
            List<Token> tokens = new List<Token>();

            Console.WriteLine("source:\n" + source);

            //Pointers  
            int lexemBegin = 0;
            int forward = 0;


            Action<int> MovePointer = (offset) =>
            {
                lexemBegin += offset;
                forward = lexemBegin + 1;
            };

            //Scan  
            while (lexemBegin != source.Length)
            {
                var seg = source.Substring(lexemBegin, forward - lexemBegin);

                Console.WriteLine("[" + seg + "]" + "(" + lexemBegin + "," + forward + ")");

                //WHITE SPACE  
                if (seg != "" && PatternMatch(seg, whitespace))
                {
                    Console.WriteLine("\n>>>>> white space. length:" + seg.Length + "\n\n");

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

                        Console.WriteLine("\n>>>>> keyword:" + keyword + "\n\n");

                        Token token = new Token(keyword, PatternType.Keyword);
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
                        Console.WriteLine("\n>>>>> operator:" + opStr + "\n\n");

                        Token token = new Token(opStr, PatternType.Operator);
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



                //ID  
                if (PatternMatch(seg, identifierPattern))
                {
                    string identifierName = seg.Substring(0, identifierPattern.back);
                    Console.WriteLine("\n>>>>> identifier:" + identifierName + "\n\n");

                    int idx = globalScope.symbolTable.AddSymbol(identifierName);
                    Token token = new Token("id", PatternType.Id, idx.ToString());
                    tokens.Add(token);

                    MovePointer(seg.Length - identifierPattern.back);
                    continue;
                }

                //NUMBERS
                if (PatternMatch(seg, numberPattern))
                {
                    string numberStr = seg.Substring(0, seg.Length - numberPattern.back);
                    Console.WriteLine("\n>>>>> number value:" + numberStr + "\n\n");

                    int idx = globalScope.symbolTable.AddSymbol(numberStr);
                    Token token = new Token("num", PatternType.Number, idx.ToString());
                    tokens.Add(token);

                    MovePointer(seg.Length - numberPattern.back);

                    continue;
                }


                forward++;
            }


            return tokens;
        }


        /// <summary>
        /// 语法分析  
        /// </summary>
        public SyntaxTree Parse(List<Token> input)
        {
            Parser parser = new Parser(input);
            parser.Parse();

            return parser.syntaxTree;
        }
    }


    /// <summary>
    /// 语法分析器  
    /// </summary>
    public class Parser
    {
        // ****** 注意事项 ******  
        //
        //非终结符的产生式的第一项是非终结符自己，会导致左递归。  
        //预测分析法要求FIRST(α)和FIRST(β)不相交。   
        //
        //
        // ****** 文法定义 ******  
        //stmt  ->  <var> <id> <=> expr <;>
        //          <id> <=> expr <;>
        //
        //
        //expr  ->  term rest  
        //          //expr + ...  （不可行）（左递归）
        //
        //term  ->  <id>
        //          <num>
        //
        //rest  ->  <+> term
        //          <-> term
        //          ε







        public List<Token> input;
        public SyntaxTree syntaxTree = null;

        private int lookahead = 0;
        private Token lookaheadToken => input[lookahead];

        private SyntaxTree.Node currentNode = null;

        public Parser(List<Token> input)
        {
            this.input = input;
            this.lookahead = 0;

            this.syntaxTree = new SyntaxTree();
            this.currentNode = syntaxTree.root;
        }

        public void Parse()
        {
            int stmtCounter = 0;
            while (lookahead <= (input.Count - 1))
            {
                if (++stmtCounter > 99) break;

                stmt();
            }
        }



        //预测分析法 - 非终结符过程    
        public void stmt()
        {
            var stmtNode = new SyntaxTree.Node() { isLeaf = false, name = "stmt" };
            syntaxTree.AppendNode(currentNode, stmtNode);
            this.currentNode = stmtNode;


            switch (lookaheadToken.name)
            {
                case "var":
                    match("var"); match("id"); match("="); expr(); match(";");
                    break;
                case "id":
                    match("id"); match("="); expr(); match(";");
                    break;
                default:
                    throw new Exception("syntax error!");
            }

            this.currentNode = stmtNode.parent;
        }
        public void expr()
        {
            var exprNode = new SyntaxTree.Node() { isLeaf = false, name = "expr" };
            syntaxTree.AppendNode(currentNode, exprNode);
            this.currentNode = exprNode;

            term(); rest();

            this.currentNode = exprNode.parent;
        }
        public void term()
        {
            var termNode = new SyntaxTree.Node() { isLeaf = false, name = "term" };
            syntaxTree.AppendNode(currentNode, termNode);
            this.currentNode = termNode;

            switch(lookaheadToken.name)
            {
                case "id":
                    match("id");
                    break;
                case "num":
                    match("num");
                    break;
                default:
                    throw new Exception("syntax error!");
            }

            this.currentNode = termNode.parent;
        }
        public void rest()
        {
            var restNode = new SyntaxTree.Node() { isLeaf = false, name = "rest" };
            syntaxTree.AppendNode(currentNode, restNode);
            this.currentNode = restNode;

            switch(lookaheadToken.name)
            {
                case "+":
                    match("+");term();
                    break;
                case "-":
                    match("-"); term();
                    break;
                default:
                    //ε
                    break;
            }


            this.currentNode = restNode.parent;
        }
        public void match(string terminal)
        {
            if (lookaheadToken.name == terminal)
            {
                var terminalNode = new SyntaxTree.Node() { isLeaf = true, name = terminal };
                syntaxTree.AppendNode(currentNode, terminalNode);

                lookahead++;
            }
            else
            {
                throw new Exception("syntax error! try match terminal:" + terminal + "  lookahead:" + lookaheadToken.name);
            }
        }
    }


    /// <summary>
    /// 语法树  
    /// </summary>
    public class SyntaxTree
    {
        public class Node
        {
            public bool isLeaf = false;
            public int depth = 0;
            public string name = "";

            public Node parent = null;
            public List<Node> children = new List<Node>();
        }

        public List<Node> allnodes;
        public Node root;


        public SyntaxTree()
        {
            allnodes = new List<Node>();
            root = new Node() { isLeaf = false };
            allnodes.Add(root);
        }


        public void AppendNode(Node parent, Node newnode)
        {
            if (allnodes.Contains(parent) == false) return;

            parent.children.Add(newnode);
            newnode.parent = parent;

            newnode.depth = parent.depth + 1;

            allnodes.Add(newnode);
        }


        public string Serialize()
        {
            System.Text.StringBuilder strb = new System.Text.StringBuilder();

            Traversal((node) => {
                string brace = "";
                for(int i = 0; i < node.depth; ++i)
                {
                    brace += "    ";
                }
                strb.AppendLine(brace + (node.isLeaf ? ("<" + node.name + ">") : node.name));
            });

            return strb.ToString();
        }

        public void Traversal(Action<Node> operation)
        {
            TraversalNode(root, operation);
        }
        private void TraversalNode(Node node, Action<Node> operation)
        {
            operation(node);

            foreach(var child in node.children)
            {
                TraversalNode(child, operation);
            }
        }
    }
}
