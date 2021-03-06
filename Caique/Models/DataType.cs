using System;
using System.Linq;
using System.Collections.Generic;

namespace Caique.Models
{
    struct DataType
    {
        public BaseType BaseType   { get; set; }
        public int      ArrayDepth { get; set; } // How many dimensions an array has, 0 means it isn't an array

        public DataType(BaseType baseType, int arrayDepth = 0)
        {
            BaseType = baseType;
            ArrayDepth = arrayDepth;
        }

        public override string ToString()
        {
            if (ArrayDepth > 0)
            {
                return BaseType.ToString() + "[" +
                    string.Concat(Enumerable.Repeat(",", ArrayDepth - 1)) + "]";
            }
            else
            {
                return BaseType.ToString();
            }
        }
    }
}
