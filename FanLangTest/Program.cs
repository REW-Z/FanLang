using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FanLang;

namespace FanLangTest
{
    class Program
    {
        static void Main(string[] args)
        {
            TestLL1Parse();
        }


        static void TestSimpleParse()
        {
            string input =
                @"var a = 233;
                  var b = 111;
                  var c = a + 999;
                 ";

            FanLang.Compiler compiler = new Compiler();

            //词法分析  
            List<Token> tokens = compiler.Scan(input);

            Console.WriteLine("\n\n词法单元列表：");
            foreach (var t in tokens)
            {
                Console.WriteLine(t.ToString());
            }


            //语法分析  
            ParseTree tree = compiler.Parse(tokens);

            Console.WriteLine("\n\n语法分析树：");
            Console.WriteLine(tree.Serialize());
        }



        static void TestLL1Parse()
        {
            string input =
                @"var a = 233;
                  var b = (a + 111) * 222;
                 ";

            FanLang.Compiler compiler = new Compiler();

            //词法分析  
            List<Token> tokens = compiler.Scan(input);

            Console.WriteLine("\n\n词法单元列表：");
            foreach (var t in tokens)
            {
                Console.WriteLine(t.ToString());
            }


            //语法分析  
            ParseTree tree = compiler.Parse(tokens);

            Console.WriteLine("\n\n语法分析树：");
            Console.WriteLine(tree.Serialize());
        }
    }
}
