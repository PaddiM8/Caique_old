using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Statements
{
    struct Argument
    {
        public BaseType Type { get; }
        public Token    Name { get; }

        public Argument(BaseType type, Token name)
        {
            this.Type = type;
            this.Name = name;
        }
    }
}
