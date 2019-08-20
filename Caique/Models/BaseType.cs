using System;
using System.Collections.Generic;
using LLVMSharp;

namespace Caique.Models
{
    public enum BaseType
    {
        Unknown,
        String, StringConst,
        Int1, Int8, Int16, Int32, Int64, Int128,
        Float16, Float32, Float64, Float80, Float128,
        Boolean, True, False,
        Void,
        Variadic
    }

    public static class BaseTypeMethods
    {
        public static LLVMTypeRef ToLLVMType(this BaseType baseType)
        {
            switch (baseType)
            {
                case BaseType.String:   return LLVM.PointerType(LLVMTypeRef.Int8Type(), 0);
                case BaseType.Int1:     return LLVM.Int1Type();
                case BaseType.Int8:     return LLVM.Int8Type();
                case BaseType.Int16:    return LLVM.Int16Type();
                case BaseType.Int32:    return LLVM.Int32Type();
                case BaseType.Int64:    return LLVM.Int64Type();
                case BaseType.Int128:   return LLVM.Int128Type();
                case BaseType.Float16:  return LLVM.HalfType();
                case BaseType.Float32:  return LLVM.FloatType();
                case BaseType.Float64:  return LLVM.DoubleType();
                case BaseType.Float80:  return LLVM.X86FP80Type();
                case BaseType.Float128: return LLVM.PPCFP128Type();
                case BaseType.True:     return LLVM.Int1Type();
                case BaseType.False:    return LLVM.Int1Type();
                default:
                    throw new Exception("Variable type can't be converted to LLVM type.");
            }
        }

        public static bool IsNumber(this BaseType baseType)
        {
            return IsInt(baseType) || IsFloat(baseType);
        }

        public static bool IsInt(this BaseType baseType)
        {
            switch (baseType)
            {
                case BaseType.Int1:
                case BaseType.Int8:
                case BaseType.Int32:
                case BaseType.Int64:
                case BaseType.Int128:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsFloat(this BaseType baseType)
        {
            switch (baseType)
            {
                case BaseType.Float16:
                case BaseType.Float32:
                case BaseType.Float64:
                case BaseType.Float80:
                case BaseType.Float128:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBool(this BaseType baseType)
        {
            return baseType == BaseType.True || baseType == BaseType.False;
        }
    }
}
