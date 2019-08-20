using System;
using System.Collections.Generic;

namespace Caique.Models
{
    struct DataType
    {
        public BaseType BaseType;
        public int      ArrayDepth; // How many dimensions an array has, 0 means it isn't an array

        public DataType(BaseType baseType, int arrayDepth = 0)
        {
            this.BaseType = baseType;
            this.ArrayDepth = arrayDepth;
        }
    }
}
