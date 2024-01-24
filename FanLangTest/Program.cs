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

            string input =
                @"var a = 233;
                  var b = 111;
                  var c = a + 999;
                 ";

            FanLang.Compiler compiler = new Compiler();
            
            //词法分析  
            List<Token> tokens = compiler.Scan(input);

            Console.WriteLine("词法单元列表：");
            foreach (var t in tokens)
            {
                Console.WriteLine(t.ToString());
            }


            //语法分析  
            SyntaxTree tree = compiler.Parse(tokens);

            Console.WriteLine("语法分析树：");
            Console.WriteLine(tree.Serialize());
        }
    }
}
