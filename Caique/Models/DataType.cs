using System;
using System.Linq;
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

        public override string ToString()
        {
            if (ArrayDepth > 0)
            {
                return BaseType.ToString() + "[" + string.Concat(Enumerable.Repeat(",", ArrayDepth - 1)) + "]";
            }
            else
            {
                return BaseType.ToString();
            }
        }
    }
}
