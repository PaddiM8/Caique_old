using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Caique.Scanning;
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
            new Compiler(string.Join(" ", args)).Compile();
            //var tokens = new Lexer(string.Join(" ", args)).ScanTokens();
            //var statements = new Parser(tokens).Parse();
            //new CodeGenerator(statements);
        }

        static void PrintJson(IExpression expr)
        {
            Console.WriteLine(JsonConvert.SerializeObject(expr));
        }
    }
}
