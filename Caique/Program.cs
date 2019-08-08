using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Caique.Scanner;
using Caique.Parsing;
using Caique.Logging;
using Caique.Expressions;

namespace Caique
{
    class Program
    {
        static void Main(string[] args)
        {
            var tokens = new Lexer(string.Join(" ", args)).ScanTokens();
            var expr = new Parser(tokens).Parse();
            PrintJson(expr);
        }

        static void PrintJson(IExpression expr)
        {
            Console.WriteLine(JsonConvert.SerializeObject(expr));
        }
    }
}
