using System;
using System.Collections.Generic;

namespace Caique.Models
{
    public struct Pos
    {
        public int Line { get; set; }
        public int Column { get; set; }

        public Pos(int line, int column)
        {
            this.Line = line;
            this.Column = column;
        }
    }
}
