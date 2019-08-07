using System;
using Caique.Scanner;
using Newtonsoft.Json;

namespace Caique
{
    class Program
    {
        static void Main(string[] args)
        {
            var tokens = new Lexer(string.Join(" ", args)).ScanTokens();
            foreach (var token in tokens)
                Console.WriteLine(JsonConvert.SerializeObject(token));
        }
    }
}
