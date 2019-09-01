using System;
using System.Collections.Generic;
using LLVMSharp;
using Caique.Expressions;

namespace Caique.Models
{
    class NamedValue
    {
        public LLVMValueRef      ValueRef   { get; }
        public List<IExpression> ArraySizes { get; }
        public int               ArrayDepth { get; }

        public NamedValue(LLVMValueRef valueRef, int arrayDepth = 0)
        {
            ValueRef = valueRef;
            ArrayDepth = arrayDepth;
        }

        public NamedValue(LLVMValueRef valueRef, List<IExpression> arraySizes)
        {
            ValueRef = valueRef;
            ArraySizes = arraySizes;
        }

        public NamedValue(LLVMValueRef valueRef)
        {
            ValueRef = valueRef;
        }
    }
}
