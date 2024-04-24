﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FanLang;
using FanLang.LRParse;
using FanLang.LALRGenerator;
using FanLang.SemanticRule;




namespace FanLang.LRParse
{
    /// <summary>
    /// 分析栈  
    /// </summary>
    public class ParseStack
    {
        private List<ParseStackElement> data = new List<ParseStackElement>();

        public int Count => data.Count;

        public int Top => data.Count - 1;

        public ParseStackElement this[int idx]
        {
            get { return data[idx]; }
            set { data[idx] = value; }
        }

        public ParseStackElement Peek()
        {
            return data[Top];
        }

        public void Push(ParseStackElement ele)
        {
            data.Add(ele);
        }
        public ParseStackElement Pop()
        {
            var result = data[Top];
            data.RemoveAt(Top);
            return result;
        }

        public List<ParseStackElement> ToList()
        {
            return data;
        }
    }

    /// <summary>
    /// 分析栈元素（状态和其他信息）    
    /// </summary>
    public class ParseStackElement
    {
        public State state;

        public Dictionary<string, object> attributes;

        public ParseStackElement(State state)
        {
            this.state = state;

            this.attributes = new Dictionary<string, object>();
        }
    }
}



namespace FanLang.LRParse
{
    public class LRParser
    {
        //上下文  
        public Compiler compilerContext;

        //输出  
        public ParseTree parseTree;
        public SyntaxTree syntaxTree;

        //语法分析器信息（由生成器生成）    
        public ParserData data;


        //状态机  
        public Queue<Token> remainingInput;//剩余输入    
        public ParseStack stack;//分析栈  
        public ParseStackElement newElement;//待入栈的产生式头元素    



        /// <summary>
        /// 构造函数  
        /// </summary>
        public LRParser(ParserData data, Compiler context)
        {
            this.data = data;
            this.compilerContext = context;
        }

        /// <summary>
        /// 语法分析  
        /// </summary>
        public void Parse(List<Token> input)
        {
            // *** 设置输入 ***  
            {
                //剩余输入队列
                this.remainingInput = new Queue<Token>();
                foreach (var token in input)
                {
                    this.remainingInput.Enqueue(token);
                }
                //添加$符号  
                if (input.LastOrDefault().name != "$")
                {
                    this.remainingInput.Enqueue(new Token("$", PatternType.Keyword));
                }
            }




            //语义动作执行器  
            SematicActionExecutor sematicActionExecutor = new SematicActionExecutor(this);

            //初始状态入栈  
            stack = new ParseStack();
            stack.Push(new ParseStackElement(data.lalrStates[0]));

            //自动机运行  
            while (remainingInput.Count > 0)
            {
                Console.WriteLine("剩余输入:" + string.Concat(remainingInput.ToArray().Select(t => t.name)));

                //当前此法单元  
                var currentToken = remainingInput.Peek();

                //查找ACTION表  
                Console.WriteLine("查询：ACTION[" + stack.Peek().state.idx + "," + currentToken.name + "]");
                var action = data.table.ACTION(stack.Peek().state.idx, currentToken.name);

                //ACTION语法分析动作    
                switch (action.type)
                {
                    //移入  
                    case ACTION_TYPE.Shift:
                        {
                            Console.WriteLine("移入状态" + action.num + "");

                            var token = remainingInput.Dequeue();

                            // *** 移入 ***  
                            var stateToPush = data.lalrStates[action.num];
                            var newEle = new ParseStackElement(stateToPush);

                            newEle.attributes["token"] = token;

                            stack.Push(newEle);
                            // ************  

                            Console.WriteLine("当前栈状态：" + string.Concat(stack.ToList().Select(s => "\n" + s.state.idx + ": \"" + s.state.name + "\"")) + "\n");
                        }
                        break;
                    //规约    
                    case ACTION_TYPE.Reduce:
                        {
                            Console.WriteLine("按产生式：" + data.productions[action.num].ToExpression() + "规约");

                            // *** 确定产生式 ***  
                            var production = data.productions[action.num];
                            // ******************

                            // *** 确定要出栈的状态数量 ***  
                            var βLength = production.body.Length;
                            // ************************

                            // *** 确定产生式头对应状态 ***  
                            var goTo = data.table.GOTO(stack[stack.Top - βLength].state.idx, production.head.name);
                            this.newElement = new ParseStackElement(data.lalrStates[goTo.stateId]);
                            // ************************


                            // *** 执行语义动作 ***    
                            //语义动作执行在物理出栈和入栈之前  
                            sematicActionExecutor.ExecuteSemanticAction(production);
                            // ********************


                            // *** 产生式体出栈 ***  
                            for (int i = 0; i < βLength; ++i)
                            {
                                stack.Pop();
                            }
                            Console.WriteLine(βLength + "个状态出栈");
                            // *********************


                            // *** 产生式头入栈 ***  
                            stack.Push(this.newElement);
                            this.newElement = null;
                            Console.WriteLine(goTo.stateId + "状态入栈");
                            // *********************


                            Console.WriteLine("当前栈状态：" + string.Concat(stack.ToList().Select(s => "\n" + s.state.idx + ": \"" + s.state.name + "\"")) + "\n");
                        }
                        break;
                    //接受  
                    case ACTION_TYPE.Accept:
                        {
                            Compiler.Pause("\n语法分析器已Accept");

                            var lastToken = remainingInput.Dequeue();

                            if (lastToken.name == "$" && remainingInput.Count == 0)
                            {
                                Console.WriteLine("语法分析成功完成！");

                                Console.WriteLine("当前栈状态：" + string.Concat(stack.ToList().Select(s => "\n" + s.state.idx + ": \"" + s.state.name + "\"")) + "\n");

                                this.parseTree = sematicActionExecutor.parseTreeBuilder.resultTree;
                                this.syntaxTree = new SyntaxTree(sematicActionExecutor.syntaxRootNode);
                                return;
                            }
                            else
                            {
                                throw new Exception("错误的Accept！");
                            }
                        }
                    //报错  
                    case ACTION_TYPE.Error:
                        throw new Exception(" 查询到ACTION_TYPE.Error！\n接收格子：[" + data.table.accState + "," + data.table.accSymbol + "]\n当前符号:" + currentToken.name + "\n当前状态:\n" + stack.Peek().state.set.ToExpression());

                }
            }
        }
    }
}





/// <summary>
/// 语义规则  
/// </summary>
namespace FanLang.SemanticRule
{
    /// <summary>
    /// 语义动作  
    /// </summary>
    public class SemanticAction
    {
        public LRParser parserContext;
        
        public Production productionContext;

        public System.Action<LRParser, Production> action;

        public SemanticAction(LRParser parser, Production production, System.Action<LRParser, Production> action)
        {
            parserContext = parser;
            productionContext = production;
            this.action = action;
        }

        public void Execute()
        {
            action.Invoke(this.parserContext, this.productionContext);
        }
    }


    /// <summary>
    /// 自底向上的语法分析树构造器  
    /// </summary>
    public class BottomUpParseTreeBuilder
    {
        // 分析树  
        public ParseTree resultTree = new ParseTree();

        // 动作构建  
        public void BuildAction(LRParser parser, Production production)
        {
            //产生式头  
            ParseTree.Node newNode = (parser.newElement.attributes["cst_node"] = new ParseTree.Node() { isLeaf = false, name = production.head.name }) as ParseTree.Node;
            resultTree.allnodes.Add(newNode);

            //产生式体  
            for (int i = 0; i < production.body.Length; ++i)
            {
                int offset = parser.stack.Count - production.body.Length;
                var ele = parser.stack[offset + i];
                var symbol = production.body[i];

                //叶子节点（终结符节点）  
                if (symbol is Terminal)
                {
                    var node = (ele.attributes["cst_node"] = new ParseTree.Node() { isLeaf = true, name = symbol.name + "," + (ele.attributes["token"] as Token).attribute }) as ParseTree.Node;

                    resultTree.allnodes.Add(node);

                    node.parent = newNode;
                    newNode.children.Add(node);
                }
                //内部节点（非终结符节点）
                else
                {
                    var node = ele.attributes["cst_node"] as ParseTree.Node;

                    node.parent = newNode;
                    newNode.children.Add(node);
                }
            }


            if (parser.remainingInput.Count == 1 && parser.remainingInput.Peek().name == "$")
            {
                Accept(parser);
            }

        }

        // 完成  
        public void Accept(LRParser parser)
        {
            //设置根节点  
            resultTree.root = parser.newElement.attributes["cst_node"] as ParseTree.Node;

            //设置深度
            resultTree.root.depth = 0;
            resultTree.Traversal((node) => {
                foreach (var c in node.children)
                {
                    c.depth = node.depth + 1;
                }
            });

            Console.WriteLine("根节点:" + resultTree.root.name);
            Console.WriteLine("节点数:" + resultTree.allnodes.Count);
        }
    }

    /// <summary>
    /// 语义动作执行器  
    /// </summary>
    public class SematicActionExecutor 
    {
        private LRParser parserContext;

        public Dictionary<Production, List<SemanticAction>> translateScheme = new Dictionary<Production, List<SemanticAction>>();


        //语法分析树构造    
        public BottomUpParseTreeBuilder parseTreeBuilder;

        //抽象语法树构造    
        public SyntaxTree.ProgramNode syntaxRootNode;

        // 构造  
        public SematicActionExecutor(LRParser parser)
        {
            this.parserContext = parser;

            //语法树构造器  
            parseTreeBuilder = new BottomUpParseTreeBuilder();

            //构建语法分析树的语义动作    
            foreach (var p in parserContext.data.productions)
            {
                this.AddActionAtTail(p, parseTreeBuilder.BuildAction);
            }

            //构建抽象语法树(AST)的语义动作   
            AddActionAtTail("S -> statements", (psr, production) =>
            {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ProgramNode()
                {

                    statementsNode = (SyntaxTree.StatementsNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };

                this.syntaxRootNode = (SyntaxTree.ProgramNode) psr.newElement.attributes["ast_node"];
            });


            AddActionAtTail("statementblock -> { statements }", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.StatementBlockNode()
                {

                    statements = ((SyntaxTree.StatementsNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"]).statements,

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("statements -> statements stmt", (psr, production) => {

                var newStmt = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top].attributes["ast_node"];

                psr.newElement.attributes["ast_node"] = (SyntaxTree.StatementsNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"];
                ((SyntaxTree.StatementsNode)psr.newElement.attributes["ast_node"]).statements.Add(newStmt);
            });
            AddActionAtTail("statements -> stmt", (psr, production) => {

                var newStmt = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top].attributes["ast_node"];

                psr.newElement.attributes["ast_node"] = new SyntaxTree.StatementsNode() {

                    attributes = psr.newElement.attributes,
                };

                ((SyntaxTree.StatementsNode)psr.newElement.attributes["ast_node"]).statements.Add(newStmt);
            });
            AddActionAtTail("statements -> ε", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.StatementsNode()
                {
                    attributes = psr.newElement.attributes,
                };
            });


            AddActionAtTail("declstatements -> declstatements declstmt", (psr, production) => {

                var newDeclStmt = (SyntaxTree.DeclareNode)psr.stack[psr.stack.Top].attributes["ast_node"];

                psr.newElement.attributes["decl_stmts"] = (List<SyntaxTree.DeclareNode>)psr.stack[psr.stack.Top - 1].attributes["decl_stmts"];

                ((List<SyntaxTree.DeclareNode>)psr.newElement.attributes["decl_stmts"]).Add(newDeclStmt);
            });

            AddActionAtTail("declstatements -> declstmt", (psr, production) => {

                var newDeclStmt = (SyntaxTree.DeclareNode)psr.stack[psr.stack.Top].attributes["ast_node"];

                psr.newElement.attributes["decl_stmts"] = new List<SyntaxTree.DeclareNode>() { newDeclStmt };
            });

            AddActionAtTail("declstatements -> ε", (psr, production) => {

                psr.newElement.attributes["decl_stmts"] = new List<SyntaxTree.DeclareNode>() { };
            });

            AddActionAtTail("stmt -> statementblock", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });

            AddActionAtTail("stmt -> declstmt", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });


            AddActionAtTail("stmt -> stmtexpr ;", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.SingleExprStmtNode()
                {
                    exprNode = (SyntaxTree.SpecialExprNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });


            AddActionAtTail("stmt -> break ;", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.BreakStmtNode()
                {
                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("stmt -> return expr ;", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ReturnStmtNode()
                {
                    returnExprNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"],
                    
                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("stmt -> while ( expr ) stmt", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.WhileStmtNode() {

                    conditionNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    stmtNode = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });


            AddActionAtTail("stmt -> for ( stmt bexpr ; stmtexpr ) stmt", (psr, production) =>
            {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ForStmtNode()
                {
                    initializerNode = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top - 5].attributes["ast_node"],
                    conditionNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 4].attributes["ast_node"],
                    iteratorNode = (SyntaxTree.SpecialExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    stmtNode = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });


            AddActionAtTail("stmt -> if ( expr ) stmt elifclauselist elseclause", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.IfStmtNode()
                {
                    conditionClauseList = new List<SyntaxTree.ConditionClauseNode>() { 
                        new SyntaxTree.ConditionClauseNode(){
                            conditionNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 4].attributes["ast_node"],
                            thenNode = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                        },
                    },
                    
                    elseClause = (SyntaxTree.ElseClauseNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };

                ((SyntaxTree.IfStmtNode)psr.newElement.attributes["ast_node"]).conditionClauseList.AddRange(
                        (List<SyntaxTree.ConditionClauseNode>)psr.stack[psr.stack.Top - 1].attributes["condition_clause_list"]
                    );
            });


            AddActionAtTail("declstmt -> type ID = expr ;", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.VarDeclareNode()
                {
                    typeNode = (SyntaxTree.TypeNode)psr.stack[psr.stack.Top - 4].attributes["ast_node"],
                    identifierNode = new SyntaxTree.IdentityNode()
                    {
                        attributes = psr.stack[psr.stack.Top - 3].attributes,
                        token = psr.stack[psr.stack.Top - 3].attributes["token"] as Token
                    },
                    initializerNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("declstmt -> type ID ( params ) { statements }", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.FuncDeclareNode()
                {
                    returnTypeNode = (SyntaxTree.TypeNode)psr.stack[psr.stack.Top - 7].attributes["ast_node"],
                    identifierNode = new SyntaxTree.IdentityNode()
                    {
                        attributes = psr.stack[psr.stack.Top - 6].attributes,
                        token = psr.stack[psr.stack.Top - 6].attributes["token"] as Token,
                    },
                    parametersNode = (SyntaxTree.ParameterListNode)psr.stack[psr.stack.Top - 4].attributes["ast_node"],
                    statementsNode = (SyntaxTree.StatementsNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("declstmt -> class ID { declstatements }", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ClassDeclareNode() {
                    classNameNode = new SyntaxTree.IdentityNode() {
                        attributes = psr.stack[psr.stack.Top - 3].attributes,
                        token = psr.stack[psr.stack.Top - 3].attributes["token"] as Token,
                    },

                    memberDelareNodes = new List<SyntaxTree.DeclareNode>(),

                    attributes = psr.newElement.attributes,
                };

                ((SyntaxTree.ClassDeclareNode)psr.newElement.attributes["ast_node"]).memberDelareNodes.AddRange(
                    (List<SyntaxTree.DeclareNode>)psr.stack[psr.stack.Top - 1].attributes["decl_stmts"]
                );
            });


            AddActionAtTail("elifclauselist -> ε", (psr, production) => {
                psr.newElement.attributes["condition_clause_list"] = new List<SyntaxTree.ConditionClauseNode>();
            });

            AddActionAtTail("elifclauselist -> elifclauselist elifclause", (psr, production) => {
                psr.newElement.attributes["condition_clause_list"] = psr.stack[psr.stack.Top - 1].attributes["condition_clause_list"];

                ((List<SyntaxTree.ConditionClauseNode>)(psr.newElement.attributes["condition_clause_list"])).Add(
                    (SyntaxTree.ConditionClauseNode)psr.stack[psr.stack.Top].attributes["ast_node"]
                    );
            });

            AddActionAtTail("elifclause -> else if ( expr ) stmt", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ConditionClauseNode()
                {
                    conditionNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    thenNode = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("elseclause -> ε", (psr, production) => {
                psr.newElement.attributes["ast_node"] = null;
            });

            AddActionAtTail("elseclause -> else stmt", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ElseClauseNode() {

                    stmt = (SyntaxTree.StmtNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });


            AddActionAtTail("assign -> lvalue = expr", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.AssignNode()
                {
                    op = "=",

                    lvalueNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    rvalueNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });
            string[] specialAssignOps = new string[] { "+=", "-=", "*=", "/=", "%="};
            foreach(var assignOp in specialAssignOps)
            {
                AddActionAtTail("assign -> lvalue " + assignOp + " expr", (psr, production) => {

                    psr.newElement.attributes["ast_node"] = new SyntaxTree.AssignNode()
                    {
                        op = assignOp,

                        lvalueNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                        rvalueNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                        attributes = psr.newElement.attributes,
                    };
                });
            }


            AddActionAtTail("lvalue -> ID", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.IdentityNode() { 
                    attributes = psr.stack[psr.stack.Top].attributes,
                    token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                };
            });

            AddActionAtTail("lvalue -> memberaccess", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.MemberAccessNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });


            AddActionAtTail("type -> ID", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ClassTypeNode() {

                    classname = new SyntaxTree.IdentityNode() { 
                        attributes = psr.stack[psr.stack.Top].attributes,
                        token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                    },

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("type -> primitive", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.PrimitiveNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });

            string[] primiveProductions = new string[] { "void", "bool", "int", "float", "string" };
            foreach(var t in primiveProductions)
            {
                AddActionAtTail("primitive -> " + t, (psr, production) => {
                    psr.newElement.attributes["ast_node"] = new SyntaxTree.PrimitiveNode()
                    {
                        attributes = psr.stack[psr.stack.Top].attributes,
                        token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                    };
                });
            }


            AddActionAtTail("expr -> assign", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });
            AddActionAtTail("expr -> nexpr", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });




            AddActionAtTail("stmtexpr -> assign", (psr, production) => {

                psr.newElement.attributes["ast_node"] = (SyntaxTree.SpecialExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });
            AddActionAtTail("stmtexpr -> call", (psr, production) => {

                psr.newElement.attributes["ast_node"] = (SyntaxTree.SpecialExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });
            AddActionAtTail("stmtexpr -> incdec", (psr, production) => {

                psr.newElement.attributes["ast_node"] = (SyntaxTree.SpecialExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });
            AddActionAtTail("stmtexpr -> newobj", (psr, production) => {

                psr.newElement.attributes["ast_node"] = (SyntaxTree.SpecialExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });




            AddActionAtTail("nexpr -> bexpr", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });
            AddActionAtTail("nexpr -> aexpr", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });

            string[] logicOperators = new string[] { "||", "&&" };
            foreach(var opname in logicOperators)
            {
                AddActionAtTail("bexpr -> bexpr " + opname + " bexpr", (psr, production) => {
                    psr.newElement.attributes["ast_node"] = new SyntaxTree.BinaryOpNode()
                    {
                        op = opname,
                        leftNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                        rightNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                        attributes = psr.newElement.attributes,
                    };
                });
            }
            string[] compareOprators = new string[] { ">", "<", ">=", "<=", "==", "!=", };
            foreach(var opname in compareOprators)
            {
                AddActionAtTail("bexpr -> aexpr " + opname + " aexpr", (psr, production) => {
                    psr.newElement.attributes["ast_node"] = new SyntaxTree.BinaryOpNode()
                    {
                        op = opname,
                        leftNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                        rightNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                        attributes = psr.newElement.attributes,
                    };
                });
            }
            AddActionAtTail("bexpr -> factor", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });




            AddActionAtTail("aexpr -> aexpr + term", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.BinaryOpNode() { 
                    op = "+",
                    leftNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    rightNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });
            AddActionAtTail("aexpr -> aexpr - term", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.BinaryOpNode()
                {
                    op = "-",
                    leftNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    rightNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });
            AddActionAtTail("aexpr -> term", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });


            AddActionAtTail("term -> term * factor", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.BinaryOpNode()
                {
                    op = "*",
                    leftNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    rightNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });
            AddActionAtTail("term -> term / factor", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.BinaryOpNode()
                {
                    op = "*",
                    leftNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    rightNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });
            AddActionAtTail("term -> factor", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });
            
            AddActionAtTail("factor -> incdec", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.SpecialExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });

            AddActionAtTail("factor -> ! factor", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.UnaryOpNode()
                {
                    op = "!",

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("factor -> - factor", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.UnaryOpNode()
                {
                    op = "-",

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("factor -> cast", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });

            AddActionAtTail("factor -> primary", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });


            AddActionAtTail("primary -> ( expr )", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"];
            });
            AddActionAtTail("primary -> ID", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.IdentityNode()
                {
                    attributes = psr.stack[psr.stack.Top].attributes,
                    token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                };
            });
            AddActionAtTail("primary -> memberaccess", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });

            AddActionAtTail("primary -> newobj", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });

            AddActionAtTail("primary -> call", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.SpecialExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });

            AddActionAtTail("primary -> lit", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"];
            });


            AddActionAtTail("incdec -> ++ ID", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.IncDecNode()
                {
                    op = "++",
                    isFront = true,
                    identifierNode = new SyntaxTree.IdentityNode()
                    {
                        attributes = psr.stack[psr.stack.Top].attributes,
                        token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                    },

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("incdec -> -- ID", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.IncDecNode()
                {
                    op = "--",
                    isFront = true,
                    identifierNode = new SyntaxTree.IdentityNode()
                    {
                        attributes = psr.stack[psr.stack.Top].attributes,
                        token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                    },

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("incdec -> ID ++", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.IncDecNode()
                {
                    op = "++",
                    isFront = false,
                    identifierNode = new SyntaxTree.IdentityNode()
                    {
                        attributes = psr.stack[psr.stack.Top - 1].attributes,
                        token = psr.stack[psr.stack.Top - 1].attributes["token"] as Token,
                    },

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("incdec -> ID --", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.IncDecNode()
                {
                    op = "--",
                    isFront = false,
                    identifierNode = new SyntaxTree.IdentityNode()
                    {
                        attributes = psr.stack[psr.stack.Top - 1].attributes,
                        token = psr.stack[psr.stack.Top - 1].attributes["token"] as Token,
                    },

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("call -> ID ( args )", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.CallNode()
                {
                    isMemberAccessFunction = false,
                    funcNode = new SyntaxTree.IdentityNode()
                    {
                        attributes = psr.stack[psr.stack.Top - 3].attributes,
                        token = psr.stack[psr.stack.Top - 3].attributes["token"] as Token,
                    },
                    argumantsNode = (SyntaxTree.ArgumentListNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"],


                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("call -> memberaccess ( args )", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.CallNode()
                {
                    isMemberAccessFunction = true,
                    funcNode = (SyntaxTree.MemberAccessNode)psr.stack[psr.stack.Top - 3].attributes["ast_node"],
                    argumantsNode = (SyntaxTree.ArgumentListNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"],


                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("cast -> ( type ) factor", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.CastNode()
                {
                    typeNode = (SyntaxTree.TypeNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    factorNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"],

                    attributes = psr.newElement.attributes,
                };
            });


            AddActionAtTail("newobj -> new ID ( )", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.NewObjectNode()
                {
                    className = new SyntaxTree.IdentityNode() { 
                        attributes = psr.stack[psr.stack.Top - 2].attributes,
                        token = psr.stack[psr.stack.Top - 2].attributes["token"] as Token,
                    },

                    attributes = psr.newElement.attributes,
                };
            });


            AddActionAtTail("memberaccess -> primary . ID", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.ObjectMemberAccessNode()
                {
                    objectNode = (SyntaxTree.ExprNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"],
                    memberNode = new SyntaxTree.IdentityNode() {
                        attributes = psr.stack[psr.stack.Top].attributes,
                        token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                    },

                    attributes = psr.newElement.attributes,
                };
            });

            AddActionAtTail("memberaccess -> this . ID", (psr, production) => {

                psr.newElement.attributes["ast_node"] = new SyntaxTree.ThisMemberAccessNode()
                {
                    memberNode = new SyntaxTree.IdentityNode()
                    {
                        attributes = psr.stack[psr.stack.Top].attributes,
                        token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                    },

                    attributes = psr.newElement.attributes,
                };
            });

            string[] litTypes = new string[] { "LITINT", "LITFLOAT", "LITSTRING", "LITBOOL" };
            foreach (var litType in litTypes)
            {

                AddActionAtTail("lit -> " + litType, (psr, production) => {
                    psr.newElement.attributes["ast_node"] = new SyntaxTree.LiteralNode()
                    {
                        attributes = psr.stack[psr.stack.Top].attributes,
                        token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                    };
                });
            }


            AddActionAtTail("params -> ε", (psr, production) =>
            {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ParameterListNode()
                {
                    parameterNodes = new List<SyntaxTree.ParameterNode>()//空的子节点  
                };
            });

            AddActionAtTail("params -> type ID", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ParameterListNode()
                {
                    parameterNodes = new List<SyntaxTree.ParameterNode>() {
                        new SyntaxTree.ParameterNode(){
                            typeNode = (SyntaxTree.TypeNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"],
                            identifierNode = new SyntaxTree.IdentityNode(){
                                attributes = psr.stack[psr.stack.Top].attributes,
                                token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                            },
                        }
                    }
                };
            });

            AddActionAtTail("params -> params , type ID", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ParameterListNode)psr.stack[psr.stack.Top - 3].attributes["ast_node"];

                ((SyntaxTree.ParameterListNode)psr.newElement.attributes["ast_node"]).parameterNodes.Add(
                    new SyntaxTree.ParameterNode()
                    {
                        typeNode = (SyntaxTree.TypeNode)psr.stack[psr.stack.Top - 1].attributes["ast_node"],
                        identifierNode = new SyntaxTree.IdentityNode()
                        {
                            attributes = psr.stack[psr.stack.Top].attributes,
                            token = psr.stack[psr.stack.Top].attributes["token"] as Token,
                        },
                    }
                );
            });

            AddActionAtTail("args -> ε", (psr, production) =>
            {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ArgumentListNode()
                {
                    arguments = new List<SyntaxTree.ExprNode>()//空的子节点  
                };
            });

            AddActionAtTail("args -> expr", (psr, production) => {
                psr.newElement.attributes["ast_node"] = new SyntaxTree.ArgumentListNode() { 
                    arguments = new List<SyntaxTree.ExprNode>() {
                        (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"]
                    }
                };
            });

            AddActionAtTail("args -> args , expr", (psr, production) => {
                psr.newElement.attributes["ast_node"] = (SyntaxTree.ArgumentListNode)psr.stack[psr.stack.Top - 2].attributes["ast_node"];

                ((SyntaxTree.ArgumentListNode)psr.newElement.attributes["ast_node"]).arguments.Add(
                    (SyntaxTree.ExprNode)psr.stack[psr.stack.Top].attributes["ast_node"]
                );
            });
        }

        // 插入语义动作
        public void AddActionAtTail(string productionExpression, System.Action<LRParser, Production> act)
        {
            Production production = parserContext.data.productions.FirstOrDefault(p => p.ToExpression() == productionExpression);

            if (production == null) throw new Exception("找不到Production：" + productionExpression);

            AddActionAtTail(production, act);
        }
        public void  AddActionAtTail(Production production, System.Action<LRParser, Production> act)
        {
            SemanticAction semanticAction = new SemanticAction(this.parserContext, production, act);

            if(translateScheme.ContainsKey(production) == false)
            {
                translateScheme[production] = new List<SemanticAction>();
            }

            translateScheme[production].Add(semanticAction) ;
        }


        // 执行语义动作  
        public void ExecuteSemanticAction(Production production)
        {
            if (translateScheme.ContainsKey(production) == false) return;

            foreach (var act in translateScheme[production])
            {
                act.Execute();
            }
        }
    }


    /// <summary>
    /// 语义分析器  
    /// </summary>
    public class SemanticAnalyzer//（补充的语义分析器，自底向上规约已经进行了部分语义分析）  
    {
        public SyntaxTree ast;

        public Compiler compilerContext;

        private FanLang.Stack<SymbolTable> tableStack = new Stack<SymbolTable>();

        /// <summary>
        /// 构造  
        /// </summary>
        public SemanticAnalyzer(SyntaxTree ast, Compiler compilerContext)
        {
            this.ast = ast;
            this.compilerContext = compilerContext;
        }

        /// <summary>
        /// 开始语义分析  
        /// </summary>
        public void Analysis()
        {
            tableStack.Push(compilerContext.globalSymbolTable);

            Analysis(ast.rootNode);
        }

        /// <summary>
        /// 递归向下语义分析（符号表初始化/类型检查等）  
        /// </summary>
        private void Analysis(SyntaxTree.Node node)
        {
            ///很多编译器从语法分析阶段甚至词法分析阶段开始初始化和管理符号表    
            ///为了降低复杂性、实现低耦合和模块化，在语义分析阶段和中间代码生成阶段管理符号表  

            /// 分析节点
            ///（符号表填充）
            ///（类型检查）
            switch (node)
            {
                case SyntaxTree.ProgramNode programNode:
                    {
                        Analysis(programNode.statementsNode);
                    }
                    break;
                case SyntaxTree.StatementsNode stmtsNode:
                    {
                        foreach(var stmtNode in stmtsNode.statements)
                        {
                            Analysis(stmtNode);
                        }
                    }
                    break;
                case SyntaxTree.StatementBlockNode stmtBlockNode:
                    {
                        //进入作用域  
                        var newEnv = new SymbolTable("stmtblock", tableStack.Peek());
                        stmtBlockNode.attributes["env"] = newEnv;
                        tableStack.Push(newEnv);

                        foreach (var stmtNode in stmtBlockNode.statements)
                        {
                            Analysis(stmtNode);
                        }

                        //离开作用域  
                        tableStack.Pop();
                    }
                    break;
                case SyntaxTree.VarDeclareNode varDeclNode:
                    {
                        //类型检查（初始值）  
                        CheckType(varDeclNode.typeNode.ToExpression(), varDeclNode.initializerNode);

                        //新建符号表条目  
                        var newRec = tableStack.Peek().NewRecord(
                            varDeclNode.identifierNode.token.attribute, 
                            SymbolTable.RecordCatagory.VariableOrParam,
                            varDeclNode.typeNode.ToExpression()
                            );
                    }
                    break;
                case SyntaxTree.FuncDeclareNode funcDeclNode:
                    {
                        //返回值类型检查（仅限非void的函数）  
                        if (!(funcDeclNode.returnTypeNode is SyntaxTree.PrimitiveNode && (funcDeclNode.returnTypeNode as SyntaxTree.PrimitiveNode).token.name == "void"))
                        {
                            //检查返回语句和返回表达式    
                            var returnStmt = funcDeclNode.statementsNode.statements.FirstOrDefault(s => s is SyntaxTree.ReturnStmtNode);
                            if (returnStmt == null) throw new Exception("类型错误：没有返回语句！");

                            CheckType(funcDeclNode.returnTypeNode.ToExpression(), (returnStmt as SyntaxTree.ReturnStmtNode).returnExprNode);
                        }


                        //记录函数符号  
                        {
                            //符号的类型表达式  
                            string typeExpr = "";
                            for (int i = 0; i < funcDeclNode.parametersNode.parameterNodes.Count; ++i)
                            {
                                if (i != 0) typeExpr += ",";
                                var paramNode = funcDeclNode.parametersNode.parameterNodes[i];
                                typeExpr += (paramNode.typeNode.ToExpression());
                            }
                            typeExpr += (" -> " + funcDeclNode.returnTypeNode.ToExpression());
                            //添加条目  
                            var newRec = tableStack.Peek().NewRecord(
                                funcDeclNode.identifierNode.token.attribute,
                                SymbolTable.RecordCatagory.Function,
                                typeExpr
                                );
                        }

                        //进入作用域  
                        var newEnv = new SymbolTable("func" + funcDeclNode.identifierNode.token.attribute, tableStack.Peek());
                        funcDeclNode.attributes["env"] = newEnv;
                        tableStack.Push(newEnv);

                        //分析形参定义和局部语句  
                        foreach (var paramNode in funcDeclNode.parametersNode.parameterNodes)
                        {
                            Analysis(paramNode);
                        }
                        foreach(var stmtNode in funcDeclNode.statementsNode.statements)
                        {
                            Analysis(stmtNode);
                        }
                        

                        //离开作用域  
                        tableStack.Pop();
                    }
                    break;
                case SyntaxTree.ParameterNode paramNode:
                    {
                        //形参加入函数作用域的符号表  
                        var newRec = tableStack.Peek().NewRecord(
                            paramNode.identifierNode.token.attribute,
                            SymbolTable.RecordCatagory.VariableOrParam,
                            paramNode.typeNode.ToExpression()
                            );
                    }
                    break;
                case SyntaxTree.AssignNode assignNode:
                    {
                        //类型检查（赋值）  
                        {
                            CheckType(assignNode.lvalueNode, assignNode.rvalueNode);
                        }
                    }
                    break;
                default:
                    break;
            }

        }


        /// <summary>
        /// 获取表达式的类型表达式  
        /// </summary>
        /// <param name="exprNode"></param>
        /// <returns></returns>
        private string GetTypeExpression(SyntaxTree.ExprNode exprNode)
        {
            switch(exprNode)
            {
                case SyntaxTree.IdentityNode idNode:
                    {
                        return null;
                    }
                case SyntaxTree.ThisMemberAccessNode accessNode:
                    {
                        return null;
                    }
                case SyntaxTree.ObjectMemberAccessNode accessNode:
                    {
                        return null;
                    }
                case SyntaxTree.CallNode callNode:
                    {
                        return null;
                    }
                default:
                    throw new Exception("无法分析类型：" + exprNode.GetType().Name);
            }
        }


        private bool CheckType(string typeExpr1, string typeExpr2)
        {
            return typeExpr1 == typeExpr2;
        }
        private bool CheckType(string typeExpr, SyntaxTree.ExprNode exprNode)
        {
            return typeExpr == GetTypeExpression(exprNode);
        }
        private bool CheckType(SyntaxTree.ExprNode exprNode1, SyntaxTree.ExprNode exprNode2)
        {
            return GetTypeExpression(exprNode1) == GetTypeExpression(exprNode2);
        }


        private SymbolTable.Record QueryRecord_In_EnvStack(string name)
        {
            var toList = tableStack.ToList();
            for (int i = toList.Count - 1; i > 0; --i)
            {
                if (toList[i].ContainRecordName(name))
                {
                    return toList[i].GetRecord(name);
                }
            }

            return null;
        }
    }
}
