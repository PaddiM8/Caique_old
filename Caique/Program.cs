using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Caique.Scanner;
using Caique.Parsing;
using Caique.Logging;
using Caique.Expressions;
using Caique.CodeGen;

namespace Caique
{
    class Program
    {
        static void Main(string[] args)
        {
            var tokens = new Lexer(string.Join(" ", args)).ScanTokens();
            var statements = new Parser(tokens).Parse();
            new CodeGenerator(statements);
            //PrintJson(expr);
        }

        static void PrintJson(IExpression expr)
        {
            Console.WriteLine(JsonConvert.SerializeObject(expr));
        }
    }
}
