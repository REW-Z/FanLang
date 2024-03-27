using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FanLang.LRParse;





namespace FanLang.LRParse
{
    /// <summary>
    /// 项目  
    /// </summary>
    public class LR1Item
    {
        public Production production;
        public int iDot;
        public Terminal lookahead;
    }

    /// <summary>
    /// 项集  
    /// </summary>
    public class LR1ItemSet : IEnumerable<LR1Item>
    {
        private List<LR1Item> items = new List<LR1Item>();

        public IEnumerator<LR1Item> GetEnumerator()
        {
            return ((IEnumerable<LR1Item>)items).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)items).GetEnumerator();
        }

        public void AddDistince(LR1Item item)
        {
            if(this.AnyRepeat(item) == false)
            {
                items.Add(item);
            }
        }
        public bool AnyRepeat(LR1Item item)
        {
            if(items.Any(i => i.production == item.production && i.iDot == item.iDot && i.lookahead == item.lookahead))
            {
                return true;
            }
            return false;
        }
        public LR1ItemSet Clone()
        {
            LR1ItemSet newCollection = new LR1ItemSet();
            foreach (var itm in this.items)
            {
                newCollection.AddDistince(itm);
            }
            return newCollection;
        }
    }


    public static class LR1ItemExtensions
    {
        public static string ToExpression(this LR1Item item)
        {
            StringBuilder strb = new StringBuilder();

            strb.Append(item.production.head.name);
            strb.Append(" -> ");
            for (int i = 0; i < item.production.body.Length; ++i)
            {
                if(i == item.iDot)
                {
                    strb.Append('·');
                }
                strb.Append(item.production.body[i]);
            }
            strb.Append(", ");
            strb.Append(item.lookahead.name);

            return strb.ToString();
        }
    }
}

namespace FanLang.LALR1Parse
{
    public enum ACTION_TYPE
    {
        Shift,  //移入(s)
        Reduce, //规约(r)
        Accept, //接受(acc)
        Error,  //报错(err)
    }
    public struct ACTION
    {
        public ACTION_TYPE type;//操作类型  
        public int num;//要移进的状态ID或者要规约的产生式ID    
    }
    public struct GOTO
    {
        public int stateId;
    }


    /// <summary>
    /// 分析表  
    /// </summary>
    public class ParseTable
    {
        //ACTION  
        private List<ACTION[]> actionRows;

        //GOTO  
        private List<GOTO[]> gotoColumns;

        //设置    
        public void SetAction(State state, Terminal symbol, Action v)
        {
        }
        public void SetGoto(State state, Nonterminal symbol, GOTO v)
        {
        }

        //压缩    
        public void Compress()
        {
        }

        public ACTION QueryAction(State i, Terminal a)
        {
            //Action[] row = i.id; 

            return new ACTION() { type = ACTION_TYPE.Error };
        }

        public GOTO QueryGoto(State i, Nonterminal A)
        {
            return default;
        }
    }

    /// <summary>
    /// 状态    
    /// </summary>
    public class State
    {
        //LR(0)自动机的每个状态都代表了规范LR(0)项集族的一个项集。  //CRE:一个状态代表一个文法符号，可以是终结符或非终结符    

        public LR1ItemSet itemCollectionClosure;
    }


    public class LALRParser
    {
        public List<Token> input;

        //文法信息  
        private Nonterminal startSymbol = null;//开始符号
        private List<Symbol> symbols = new List<Symbol>();//符号列表   
        private List<Nonterminal> nonterminals = new List<Nonterminal>();//非终结符列表  
        private List<Terminal> terminals = new List<Terminal>();//终结符列表  
        private List<Production> productions = new List<Production>();  //产生式列表（索引代表产生式编号）  
        private List<LR1Item> items = new List<LR1Item>();//Item列表  

        //项和产生式查询表  
        private Dictionary<string, Production> productionDic = new Dictionary<string, Production>();
        private Dictionary<string, LR1Item> itemDic = new Dictionary<string, LR1Item>();



        //输出  
        public ParseTree syntaxTree;

        //G'项集族  
        public List<LR1ItemSet> canonicalItemCollection; // 即C  

        //LR自动机  
        public List<State> states = new List<State>();//所有状态（索引代表状态号）  

        public Queue<Terminal> remainingInput;

        public Stack<State> stack;

        public ParseTable table;

        //符号集缓存  
        private Dictionary<string, TerminalCollection> cachedFIRSTOfSymbolStr = new Dictionary<string, TerminalCollection>();



        // ------------------------------------- 构造函数 ------------------------------------------
        public LALRParser(List<Token> input)
        {
            this.input = input;

            this.syntaxTree = new ParseTree();


            //文法初始化  
            InitGrammar();
        }


        // ------------------------------------- 编译接口 ------------------------------------------
        //自动机运行  
        public void Run()
        {
            var token = remainingInput.Dequeue();

            var action = table.QueryAction(stack.Peek(), remainingInput.Peek());

            switch (action.type)
            {
                case ACTION_TYPE.Shift:
                    {
                        remainingInput.Dequeue();

                        var stateToPush = states[action.num];

                        stack.Push(stateToPush);
                    }
                    break;
                case ACTION_TYPE.Reduce:
                    {
                        var production = productions[action.num];

                        var βLength = 0;// ?? production.body.Length ??  

                        for (int i = 0; i < βLength; ++i)
                            stack.Pop();

                        var goTo = table.QueryGoto(stack.Peek(), production.head);

                        stack.Push(states[goTo.stateId]);

                        //执行语义动作，生成输出...
                    }
                    break;
                case ACTION_TYPE.Accept:
                    {
                        Console.WriteLine("Accept");
                    }
                    break;
                case ACTION_TYPE.Error:
                    {
                        Console.WriteLine("Error");
                    }
                    break;
            }
        }
        public void Compile()
        {
        }


        // ------------------------------------- 初始化 ------------------------------------------
        
        public void InitGrammar()
        {
            //文法初始化  
            startSymbol = NewNonterminal("stmt");
            NewNonterminal("expr");
            NewNonterminal("term");
            NewNonterminal("factor");

            NewTerminal("var");
            NewTerminal("id");
            NewTerminal("num");
            NewTerminal("=");
            NewTerminal("+");
            NewTerminal("-");
            NewTerminal("/");
            NewTerminal("*");
            NewTerminal("(");
            NewTerminal(")");
            NewTerminal(";");

            NewTerminal("$");//结束符  

            NewProduction("stmt -> var id = expr ;");
            NewProduction("stmt -> id = expr ;");
            NewProduction("expr -> expr + term");
            NewProduction("expr -> expr - term");
            NewProduction("expr -> term");
            NewProduction("term -> term * factor");
            NewProduction("term -> term / factor");
            NewProduction("term -> factor");
            NewProduction("factor -> ( expr )");
            NewProduction("factor -> id");
            NewProduction("factor -> num");


            //文法增广  
            startSymbol = NewNonterminal(@"S'");
            NewProduction(@"S' -> stmt");


            //FIRST集计算（从开始符号递归）  
            InitFIRSTCollections();


            //构造SLR(1)语法分析表
            ParseTable slrTable = new ParseTable();

            //1. 构造规范LR(0)项集族  
            InitCanonicalItemsCollection();
            InitStates();



            for (int i = 0; i < this.canonicalItemCollection.Count; ++i) 
            {
                Console.WriteLine("I" + i + ":");
                foreach(var itm in this.canonicalItemCollection[i])
                {
                    Console.WriteLine(itm.ToExpression());
                }
            }

            table = slrTable;
        }

        private Terminal NewTerminal(string name)
        {
            Terminal terminal = new Terminal() { name = name };

            symbols.Add(terminal);
            terminals.Add(terminal);

            return terminal;
        }

        private Nonterminal NewNonterminal(string name)
        {
            Nonterminal nonterminal = new Nonterminal() { name = name, productions = new List<Production>() };

            symbols.Add(nonterminal);
            nonterminals.Add(nonterminal);

            return nonterminal;
        }

        private void NewProduction(string expression)
        {
            string[] segments = expression.Split(' ');
            if (segments.Length < 2) return;
            if (segments[1] != "->") return;

            int bodyLength = segments.Length - 2;

            Nonterminal head = nonterminals.FirstOrDefault(s => s.name == segments[0]);

            if (head.productions == null) head.productions = new List<Production>();


            //是ε产生式  
            if (segments.Length == 2)
            {
                Production newProduction = new Production();
                newProduction.head = head;
                newProduction.body = new Symbol[1];
                head.productions.Add(newProduction);
                productions.Add(newProduction);
                productionDic[expression] = newProduction;

                newProduction.body[0] = null;//ε  

            }
            //不是ε产生式  
            else
            {
                Production newProduction = new Production();
                newProduction.head = head;
                newProduction.body = new Symbol[bodyLength];
                head.productions.Add(newProduction);
                productions.Add(newProduction);
                productionDic[expression] = newProduction;

                for (int i = 2; i < segments.Length; ++i)
                {
                    Symbol symbol = symbols.FirstOrDefault(s => s.name == segments[i]);
                    newProduction.body[i - 2] = symbol;
                }
            }
        }

        private void NewLR1Item(string expression) //形式: "A -> α·β, a"
        {
            string[] productionAndLookahead = expression.Split(',');
            string lookaheadStr = productionAndLookahead[1].Trim();  //a
            string itemNonLookahead = productionAndLookahead[0].Trim();  //A -> α·β

            string[] segments = itemNonLookahead.Split(' ');
            if (segments[1] != "->") { throw new Exception("LR1项格式错误"); }

            //find production
            string productionExpression = itemNonLookahead.Replace("·", "");
            if (productionDic.ContainsKey(productionExpression) == false) { throw new Exception("没找到产生式:" + productionExpression); }
            Production production = productionDic[productionExpression];

            //Find iDot  
            var itembody = segments[2].Trim();//α·β  
            int idot = itembody.IndexOf('·');

            //lookahead  
            Terminal lookahead = this.terminals.FirstOrDefault(t => t.name == lookaheadStr);
            if (lookahead == null) { throw new Exception("没找到终结符:" + lookaheadStr); }
        }

        public Production GetProduction(string expression)
        {
            if (productionDic.ContainsKey(expression))
            {
                return productionDic[expression];
            }
            else
            {
                throw new Exception("没招到产生式:" + expression);
            }
        }
        public LR1Item GetOrCreateLR1Item(string expression)
        {
            if(itemDic.ContainsKey(expression))
            {
                return itemDic[expression];
            }
            else
            {
                NewLR1Item(expression);
                return itemDic[expression];
            }
        }


        private void InitFIRSTCollections()
        {
            FIRST(startSymbol);
            foreach (var nt in nonterminals)
            {
                FIRST(nt);
            }
        }


        /* 伪代码：项集族计算  
        void items(G')
        {
            将C初始化为{CLOSURE}({[S' -> ·S, $]});
            repeat
                for(C中每个项集I)
                    for(每个文法符号X)
                        if(GOTO(I,X)非空且不在C中)
                            将GOTO(I,X)加入C中;
            until(不再有新的项集加入到C中);
        }*/

        private void InitCanonicalItemsCollection()
        {
            List<LR1ItemSet> C = new List<LR1ItemSet>();
            LR1ItemSet initSet = new LR1ItemSet(); initSet.AddDistince(GetOrCreateLR1Item(@"S' -> S , $"));
            initSet = CLOSURE(initSet);
            C.Add(initSet);


            while(true)
            {
                bool anyAdded = false;

                //for(C中每个项集I)
                foreach (var I in C)
                {
                    //for(每个文法符号X)
                    foreach (var X in this.symbols)//文法符号X可以是终结符和非终结符  
                    {
                        //if (GOTO(I, X)非空且不在C中)
                        var gotoix = GOTO(I, X);
                        if (gotoix != null && C.Contains(gotoix) == false)
                        {
                            //将GOTO(I, X)加入C中;
                            C.Add(gotoix);
                            anyAdded = true;
                        }
                    }
                }


                if (anyAdded == false) break;
            }



            this.canonicalItemCollection = C;
        }


        private void InitStates()
        {

        }

        // ---------------------------------- 函数 -----------------------------------------

        private TerminalCollection FIRST(Symbol symbol)
        {
            //无缓存 -> 计算  
            if (symbol.cachedFIRST == null)
            {
                TerminalCollection firstCollection = new TerminalCollection();

                //终结符
                if (symbol is Terminal)
                {
                    firstCollection.AddDistinct(symbol as Terminal);
                }
                //非终结符
                else
                {
                    Nonterminal nt = symbol as Nonterminal;
                    foreach (var production in nt.productions)
                    {
                        firstCollection.AddCollection(FIRST(production.body));
                    }
                }

                symbol.cachedFIRST = (firstCollection);

                return symbol.cachedFIRST;
            }
            //有缓存 -> 读取缓存
            else
            {
                return symbol.cachedFIRST;
            }
        }

        private TerminalCollection FIRST(IEnumerable<Symbol> sstr)
        {
            string key = string.Concat(sstr.Select(s => (s != null ? s.name : "ε") + " "));

            //有缓存 -> 返回缓存  
            if (cachedFIRSTOfSymbolStr.ContainsKey(key))
            {
                return cachedFIRSTOfSymbolStr[key];
            }
            //无缓存 -> 计算并缓存  
            else
            {
                TerminalCollection firstCollection = new TerminalCollection();
                foreach (var s in sstr)
                {
                    if (s is Nonterminal)
                    {
                        //该产生式中的该非终结符 可以 推导ε
                        if ((s as Nonterminal).HasεProduction() == true)
                        {
                            firstCollection.AddCollection(FIRST(s));

                            continue;//跳到产生式下一个符号  
                        }
                        //该产生式中的该非终结符 不可以 推导ε
                        else
                        {
                            firstCollection.AddCollection(FIRST(s));

                            break;//跳出该产生式  
                        }
                    }
                    else if (s is Terminal)
                    {
                        firstCollection.AddDistinct(s as Terminal);
                        break;//跳出该产生式  
                    }
                }

                //缓存  
                cachedFIRSTOfSymbolStr[key] = firstCollection;


                return firstCollection;
            }


        }



        /* CLOSURE伪代码  
        SetOfItems CLOSURE(I)
        {
            repeat
                for(I中的每个项[A->α·Bβ,a])
                    for(G'中每个产生式B->γ)
                        for(FIRST(βa)中的每个终结符b)
                            将[B->·γ, b]加入到集合I中;
            until(不能向I中加入更多项);  
            return I;
        }*/

        public LR1ItemSet CLOSURE(LR1ItemSet oldI)
        {
            LR1ItemSet I = oldI.Clone();

            for (int i = 0; i < 9999; ++i)
            {
                bool anyAdded = false;

                foreach(var itm in I)
                {
                    //判断是不是α·Bβ
                    if ((itm.production.body[itm.iDot] is Nonterminal) == false) continue;

                    //非终结符B  
                    Nonterminal B = itm.production.body[itm.iDot] as Nonterminal;
                    Terminal a = itm.lookahead;
                    List<Symbol> β = itm.production.body.Skip(4444).ToList();  
                    List<Symbol> βa = new List<Symbol>(); βa.AddRange(β); βa.Add(a);

                    //非终结符B的所有产生式  
                    Production[] productionsOfB = productions.Where(p => p.head == B).ToArray();
                    foreach(var production in productionsOfB)
                    {
                        TerminalCollection firstβa = FIRST(βa);
                        foreach(var b in firstβa.ToArray())
                        {
                            var newItem = new LR1Item() {
                                production = production,
                                iDot = 0,
                                lookahead = b
                            };
                            if(I.AnyRepeat(newItem) == false)
                            {
                                anyAdded = true;
                                I.AddDistince(newItem);
                            }
                        }
                    }
                }

                if(anyAdded == false)
                {
                    break;
                }
            }

            return I;
        }

        /* GOTO伪代码  
        SetOfItems GOTO(I, X)
        {
            将J初始化为空集;
            for(I中的每个项[A->α·Xβ, a])
                将项[A->αX·β, a]加入到集合J中;
            return CLOSURE(J);
        }
        */

        public LR1ItemSet GOTO(LR1ItemSet I, Symbol X)//文法符号X可以是终结符和非终结符  
        {
            LR1ItemSet J = new LR1ItemSet();

            foreach(var item in I)
            {
                if (item.production.body[item.iDot] != X) continue;//过滤不是[A->α·Xβ, a]的项  

                //ADD  
                LR1Item itmAdd = new LR1Item {
                    production = item.production,
                    iDot = item.iDot + 1,
                    lookahead = item.lookahead
                };
                if(J.AnyRepeat(itmAdd) == false)
                {
                    J.AddDistince(itmAdd);
                }
            }

            return CLOSURE(J);
        }


    }
}



//保证GOTO(I,X)的结果不重复（因为会被添加到C中）  