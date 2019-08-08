using System;
using Newtonsoft.Json;
using Caique.Scanner;
using Caique.Parsing;
using Caique.Logging;

namespace Caique
{
    class Program
    {
        static void Main(string[] args)
        {
            var tokens = new Lexer(string.Join(" ", args)).ScanTokens();
            var expr = new Parser(tokens).Parse();
            //Console.WriteLine(JsonConvert.SerializeObject(expr));
            string tree = new AstPrinter().Print(expr);
            Console.WriteLine(tree);
        }
    }
}
