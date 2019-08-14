using System;
using System.Collections.Generic;
using LLVMSharp;

namespace Caique.Models
{
    public enum DataType
    {
        Unknown, String, Int, Double, Boolean, StringConst, Void, Variadic
    }

    public static class DataTypeMethods
    {
        public static LLVMTypeRef ToLLVMType(this DataType dataType)
        {
            switch (dataType)
            {
                case DataType.String:
                    return LLVM.PointerType(LLVMTypeRef.Int8Type(), 0);
                case DataType.Int:
                    return LLVM.Int32Type();
                case DataType.Double:
                    return LLVM.DoubleType();
                case DataType.Boolean:
                    return LLVM.Int1Type();
                default:
                    throw new Exception("Variable type can't be converted to LLVM type.");
            }
        }
    }
}
