using System;
using System.Collections.Generic;
using LLVMSharp;
using Caique.Models;

namespace Caique.CodeGen
{
    class TypeResolver
    {
        private LLVMBuilderRef _builder { get; }

        public TypeResolver(LLVMBuilderRef builder)
        {
            this._builder = builder;
        }

        public LLVMValueRef CastIfNecessary(LLVMTypeRef targetType, LLVMValueRef value, DataType valueDataType)
        {
            LLVMTypeRef valueType = value.TypeOf();
            if (targetType.Equals(valueType)) return value;

            // Try to cast
            LLVMTypeRef stringType = DataType.String.ToLLVMType();
            if (targetType.Equals(stringType) &&
                valueDataType == DataType.String)
            {
                return LLVM.BuildIntCast(_builder, value, stringType, "tmpcast");
            }

            throw new Exception("Invalid datatypes.");
        }
    }
}
