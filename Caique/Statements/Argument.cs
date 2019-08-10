using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Statements
{
    struct Argument
    {
        public DataType Type { get; }
        public Token    Name { get; }

        public Argument(DataType type, Token name)
        {
            this.Type = type;
            this.Name = name;
        }
    }
}
