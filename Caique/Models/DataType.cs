using System;
using System.Collections.Generic;
using LLVMSharp;

namespace Caique.Models
{
    public enum DataType
    {
        Unknown,
        String, StringConst,
        Int1, Int8, Int16, Int32, Int64, Int128,
        Float16, Float32, Float64, Float80, Float128,
        Boolean, True, False,
        Void,
        Variadic
    }

    public static class DataTypeMethods
    {
        public static LLVMTypeRef ToLLVMType(this DataType dataType)
        {
            switch (dataType)
            {
                case DataType.String:   return LLVM.PointerType(LLVMTypeRef.Int8Type(), 0);
                case DataType.Int1:     return LLVM.Int1Type();
                case DataType.Int8:     return LLVM.Int8Type();
                case DataType.Int16:    return LLVM.Int16Type();
                case DataType.Int32:    return LLVM.Int32Type();
                case DataType.Int64:    return LLVM.Int64Type();
                case DataType.Int128:   return LLVM.Int128Type();
                case DataType.Float16:  return LLVM.HalfType();
                case DataType.Float32:  return LLVM.FloatType();
                case DataType.Float64:  return LLVM.DoubleType();
                case DataType.Float80:  return LLVM.X86FP80Type();
                case DataType.Float128: return LLVM.PPCFP128Type();
                case DataType.True:     return LLVM.Int1Type();
                case DataType.False:    return LLVM.Int1Type();
                default:
                    throw new Exception("Variable type can't be converted to LLVM type.");
            }
        }

        public static bool IsNumber(this DataType dataType)
        {
            return IsInt(dataType) || IsFloat(dataType);
        }

        public static bool IsInt(this DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Int1:
                case DataType.Int8:
                case DataType.Int32:
                case DataType.Int64:
                case DataType.Int128:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsFloat(this DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Float16:
                case DataType.Float32:
                case DataType.Float64:
                case DataType.Float80:
                case DataType.Float128:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBool(this DataType dataType)
        {
            return dataType == DataType.True || dataType == DataType.False;
        }
    }
}
