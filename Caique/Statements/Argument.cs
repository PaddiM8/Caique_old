using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Statements
{
    struct Argument
    {
        public DataType DataType { get; }
        public Token    Name { get; }

        public Argument(DataType dataType, Token name)
        {
            this.DataType = dataType;
            this.Name = name;
        }
    }
}
