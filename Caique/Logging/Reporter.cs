using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Logging
{
    static class Reporter
    {
        public static readonly List<Tuple<Pos, string>> ErrorList =
            new List<Tuple<Pos, string>>();
        public static void Error(Pos pos, string message)
        {
            ErrorList.Add(new Tuple<Pos, string>(pos, message));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{pos.Line}, {pos.Column}] Error: {message}");
            Console.ResetColor();
        }
    }
}
