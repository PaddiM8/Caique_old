using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Logging
{
    static class Reporter
    {
        public static void Error(Pos pos, string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{pos.Line}, {pos.Column}] Error: {message}");
            Console.ResetColor();
        }
    }
}
