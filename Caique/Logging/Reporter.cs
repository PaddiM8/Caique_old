using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Logging
{
    static class Reporter
    {
        public static void Error(Pos pos, string message)
        {
            Console.WriteLine($"[{pos.Line}, {pos.Column}] {message}");
        }
    }
}
