using System;
using System.Collections.Generic;
using LLVMSharp;
using Caique.Models;

namespace Caique.CodeGen
{
    class LLVMHelper
    {
        private LLVMBuilderRef _builder { get; }
        private delegate LLVMValueRef BuildFunc(LLVMValueRef left, TokenType operatorType, LLVMValueRef right);
        private delegate LLVMValueRef BuildBinaryFunc(LLVMBuilderRef builder, LLVMValueRef left, LLVMValueRef right, string name);

        public LLVMHelper(LLVMBuilderRef builder)
        {
            this._builder = builder;
        }

        public LLVMValueRef BuildBinary(LLVMValueRef leftVal, TokenType operatorType, LLVMValueRef rightVal, DataType dataType)
        {
            BuildFunc buildFunc;
            if (dataType.IsInt() || dataType == DataType.Boolean)
            {
                if      (operatorType.IsArithmeticOperator())  buildFunc = BuildArithmetic;
                else if (operatorType.IsComparisonOperator())  buildFunc = BuildComparison;
                else if (operatorType.IsConjunctionOperator()) buildFunc = BuildConjunction;
                else throw new Exception($"Unexpected data type {dataType}.");
            }
            else if (dataType.IsFloat())
            {
                if      (operatorType.IsArithmeticOperator()) buildFunc = BuildFArithmetic;
                else if (operatorType.IsComparisonOperator()) buildFunc = BuildFComparison;
                else throw new Exception($"Unexpected data type {dataType}.");
            }
            else
            {
                throw new Exception($"Unexpected data type {dataType}.");
            }

            return buildFunc(leftVal, operatorType, rightVal);
        }

        public LLVMValueRef BuildArithmetic(LLVMValueRef leftVal, TokenType operatorType, LLVMValueRef rightVal)
        {
            BuildBinaryFunc buildBinaryFunc;
            switch (operatorType)
            {
                case TokenType.Plus:  buildBinaryFunc = LLVM.BuildAdd; break;
                case TokenType.Minus: buildBinaryFunc = LLVM.BuildSub; break;
                case TokenType.Star:  buildBinaryFunc = LLVM.BuildMul; break;
                default: throw new Exception("Invalid arithmetic token for type integer.");
            }

            return buildBinaryFunc(_builder, leftVal, rightVal, "aritmp");

        }

        public LLVMValueRef BuildFArithmetic(LLVMValueRef leftVal, TokenType operatorType, LLVMValueRef rightVal)
        {
            BuildBinaryFunc buildBinaryFunc;
            switch (operatorType)
            {
                case TokenType.Plus:  buildBinaryFunc = LLVM.BuildFAdd; break;
                case TokenType.Minus: buildBinaryFunc = LLVM.BuildFSub; break;
                case TokenType.Star:  buildBinaryFunc = LLVM.BuildFMul; break;
                default: throw new Exception("Invalid arithmetic token for type integer.");
            }

            return buildBinaryFunc(_builder, leftVal, rightVal, "aritmp");

        }

        public LLVMValueRef BuildComparison(LLVMValueRef leftVal, TokenType operatorType, LLVMValueRef rightVal)
        {
            LLVMIntPredicate predicate;
            switch (operatorType)
            {
                case TokenType.EqualEqual:   predicate = LLVMIntPredicate.LLVMIntEQ;  break;
                case TokenType.NotEqual:     predicate = LLVMIntPredicate.LLVMIntNE;  break;
                case TokenType.Greater:      predicate = LLVMIntPredicate.LLVMIntSGT; break;
                case TokenType.GreaterEqual: predicate = LLVMIntPredicate.LLVMIntSGE; break;
                case TokenType.Less:         predicate = LLVMIntPredicate.LLVMIntSLT; break;
                case TokenType.LessEqual:    predicate = LLVMIntPredicate.LLVMIntSLE; break;
                default: throw new Exception("Invalid comparison token for type 'int'.");
            }

            return LLVM.BuildICmp(_builder, predicate, leftVal, rightVal, "cmptmp");
        }

        public LLVMValueRef BuildFComparison(LLVMValueRef leftVal, TokenType operatorType, LLVMValueRef rightVal)
        {
            LLVMRealPredicate predicate;
            switch (operatorType)
            {
                case TokenType.EqualEqual:   predicate = LLVMRealPredicate.LLVMRealOEQ;  break;
                case TokenType.NotEqual:     predicate = LLVMRealPredicate.LLVMRealONE;  break;
                case TokenType.Greater:      predicate = LLVMRealPredicate.LLVMRealOGT; break;
                case TokenType.GreaterEqual: predicate = LLVMRealPredicate.LLVMRealOGE; break;
                case TokenType.Less:         predicate = LLVMRealPredicate.LLVMRealOLT; break;
                case TokenType.LessEqual:    predicate = LLVMRealPredicate.LLVMRealOLE; break;
                default: throw new Exception("Invalid comparison token for type 'int'.");
            }

            return LLVM.BuildFCmp(_builder, predicate, leftVal, rightVal, "cmptmp");
        }

        public LLVMValueRef BuildConjunction(LLVMValueRef leftVal, TokenType operatorType, LLVMValueRef rightVal)
        {
            if (operatorType == TokenType.Or)
            {
                return LLVM.BuildOr(_builder, leftVal, rightVal, "ortmp");
            }

            if (operatorType == TokenType.And)
            {
                return LLVM.BuildAnd(_builder, leftVal, rightVal, "andtmp");
            }

            throw new Exception($"Unexpected operator {operatorType}.");
        }
    }
}
