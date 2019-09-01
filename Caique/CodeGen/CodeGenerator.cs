using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using Newtonsoft.Json;
using Caique.Models;
using Caique.Expressions;
using Caique.Statements;
using Caique.Logging;

namespace Caique.CodeGen
{
    class CodeGenerator : IExpressionVisitor<LLVMValueRef>, IStatementVisitor<object>
    {

        // Classes
        private LLVMModuleRef  _module     { get; }
        private LLVMBuilderRef _builder    { get; }
        private LLVMHelper     _llvmHelper { get; }

        // LLVMBool
        private static readonly LLVMBool LLVMBoolFalse = new LLVMBool(0);
        private static readonly LLVMBool LLVMBoolTrue  = new LLVMBool(1);

        // Data structures
        private readonly Dictionary<string, NamedValue> _namedValues
            = new Dictionary<string, NamedValue>();
        private readonly Stack<Tuple<LLVMValueRef, BlockStmt>> _valueStack
            = new Stack<Tuple<LLVMValueRef, BlockStmt>>();
        private List<IStatement> _statements { get; }

        public CodeGenerator(List<IStatement> statements)
        {
            Console.WriteLine(JsonConvert.SerializeObject(statements));
            _module = LLVM.ModuleCreateWithName("main");
            _builder = LLVM.CreateBuilder();
            _statements = statements;
            _llvmHelper = new LLVMHelper(_builder);
        }

        public LLVMModuleRef GenerateLLVM()
        {

            LLVMTypeRef stringType = LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0);

            var printfArguments = new LLVMTypeRef[] { stringType };
            var printf = LLVM.AddFunction(_module, "printf", LLVM.FunctionType(LLVMTypeRef.Int32Type(), printfArguments, LLVMBoolTrue));
            LLVM.SetLinkage(printf, LLVMLinkage.LLVMExternalLinkage);

            var scanfArguments = new LLVMTypeRef[] { stringType };
            var scanf = LLVM.AddFunction(_module, "scanf", LLVM.FunctionType(LLVMTypeRef.Int32Type(), scanfArguments, LLVMBoolTrue));
            LLVM.SetLinkage(scanf, LLVMLinkage.LLVMExternalLinkage);

            // Generate functions, globals, etc.
            for (int i = 0; i < _statements.Count; i++)
            {
                _statements[i].Accept(this);
            }

            // Generate everything inside the functions
            while (_valueStack.Count > 0)
            {
                _valueStack.Peek().Item2.Accept(this);
            }

            return _module;
        }

        public object Visit(VarDeclarationStmt stmt)
        {
            LLVMTypeRef type = stmt.DataType.BaseType.ToLLVMType();
            LLVMValueRef alloca;

            // If not null or empty, meaning it is an array
            if (stmt.ArraySizes != null && stmt.ArraySizes.Count > 0)
            {
                // Generate code for each size expression
                LLVMValueRef[] sizeValueRefs = new LLVMValueRef[stmt.ArraySizes.Count];
                for (int k = 0; k < sizeValueRefs.Length; k++)
                {
                    sizeValueRefs[k] = stmt.ArraySizes[k].Accept(this);
                }

                LLVMValueRef size = sizeValueRefs[0];
                for (int i = 1; i < stmt.ArraySizes.Count; i++)
                {
                    size = LLVM.BuildMul(_builder, size, sizeValueRefs[i], "multmp");
                }

                // Allocate some extra space to specify the array length(s).
                size = LLVM.BuildAdd(_builder, size,
                                     LLVM.ConstInt(LLVM.Int32Type(), (ulong)stmt.ArraySizes.Count,
                                     LLVMBoolFalse), "addtmp");
                alloca = LLVM.BuildArrayAlloca(_builder, type, size, stmt.Identifier.Lexeme);

                // Store array length(s) at the start of the array.
                for (int j = 0; j < sizeValueRefs.Length; j++)
                {
                    var indices = new LLVMValueRef[]
                    {
                        LLVM.ConstInt(LLVM.Int32Type(), (ulong)j, LLVMBoolFalse)
                    };
                    LLVMValueRef gep = LLVM.BuildGEP(_builder, alloca, indices, "geptmp");
                    LLVM.BuildStore(_builder, sizeValueRefs[j], gep);
                }
            }
            else
            {
                alloca = LLVM.BuildAlloca(_builder, type, stmt.Identifier.Lexeme); // Allocate variable
            }

            var namedValue = stmt.ArraySizes == null
                             ? new NamedValue(alloca, 0)
                             : new NamedValue(alloca, stmt.ArraySizes.Count);
            _namedValues.Add(stmt.Identifier.Lexeme, namedValue); // Add to dictionary

            if (stmt.Value != null)
            {
                LLVMValueRef initializer = stmt.Value.Accept(this);
                LLVM.BuildStore(_builder, initializer, alloca);
            }

            return null;
        }

        public object Visit(AssignmentStmt stmt)
        {
            LLVMValueRef value = stmt.Value.Accept(this); // Get value after '='

            NamedValue namedValue;
            if (_namedValues.TryGetValue(stmt.Identifier.Lexeme, out namedValue)) // Get variable reference
            {
                LLVMValueRef varRef = namedValue.ValueRef;
                if (stmt.ArrayIndexes != null && stmt.ArrayIndexes.Count > 0) // If array
                {
                    varRef = GetArrayItem(varRef, namedValue.ArrayDepth, stmt.ArrayIndexes);
                }

                LLVM.BuildStore(_builder, value, varRef);
            }
            else
            {
                Reporter.Error(stmt.Identifier.Position,
                        $"Variable {stmt.Identifier.Lexeme} has not been declared.");
            }

            return null;
        }

        public object Visit(FunctionStmt stmt)
        {
            var arguments = new LLVMTypeRef[stmt.Arguments.Count];

            // Add argument types
            for (int i = 0; i < stmt.Arguments.Count; i++)
            {
                arguments[i] = stmt.Arguments[i].DataType.BaseType.ToLLVMType();
            }

            LLVMTypeRef functionType = LLVM.FunctionType(stmt.ReturnType.BaseType.ToLLVMType(), arguments, LLVMBoolFalse);
            LLVMValueRef function = LLVM.AddFunction(_module, stmt.Name.Lexeme, functionType);

            // Add names to arguments and allocate memory
            for (int k = 0; k < arguments.Length; k++)
            {
                string argumentName = stmt.Arguments[k].Name.Lexeme;
                LLVMValueRef param = LLVM.GetParam(function, (uint)k);
                LLVM.SetValueName(param, argumentName);

                _namedValues[argumentName] = new NamedValue(param);
            }

            _valueStack.Push(new Tuple<LLVMValueRef, BlockStmt>(function, stmt.Block));

            return null;
        }

        public object Visit(BlockStmt stmt)
        {
            if (_valueStack.Count > 0)
            {
                LLVMBasicBlockRef block = LLVM.AppendBasicBlock(_valueStack.Pop().Item1, "entry");
                LLVM.PositionBuilderAtEnd(_builder, block);
            }

            foreach (IStatement subStmt in stmt.Statements)
            {
                subStmt.Accept(this);
            }

            return null;
        }

        public object Visit(ReturnStmt stmt)
        {
            LLVMValueRef exprValue = stmt.Expression.Accept(this);
            exprValue = CastIfNeeded(exprValue, stmt.Expression.Cast);

            LLVM.BuildRet(_builder, exprValue);

            return null;
        }

        public object Visit(IfStmt stmt)
        {
            LLVMValueRef condition = stmt.Condition.Accept(this);
            LLVMValueRef func = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(_builder));

            // Blocks
            LLVMBasicBlockRef thenBB = LLVM.AppendBasicBlock(func, "then");
            LLVMBasicBlockRef elseBB = LLVM.AppendBasicBlock(func, "else");
            LLVMBasicBlockRef mergeBB = LLVM.AppendBasicBlock(func, "ifcont");

            // Build condition
            LLVM.BuildCondBr(_builder, condition, thenBB, elseBB);

            // Then branch
            LLVM.PositionBuilderAtEnd(_builder, thenBB); // Position builder at block
            stmt.ThenBranch.Accept(this); // Generate branch code
            LLVM.BuildBr(_builder, mergeBB); // Redirect to merge

            // Else branch
            LLVM.PositionBuilderAtEnd(_builder, elseBB); // Position builder at block
            if (stmt.ElseBranch != null) stmt.ElseBranch.Accept(this); // Generate branch code if else statement is present
            LLVM.BuildBr(_builder, mergeBB); // Redirect to merge

            LLVM.PositionBuilderAtEnd(_builder, mergeBB);

            return null;
        }

        public object Visit(WhileStmt stmt)
        {
            LLVMValueRef func = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(_builder));

            // Blocks
            LLVMBasicBlockRef condBB = LLVM.AppendBasicBlock(func, "cond");
            LLVMBasicBlockRef branchBB = LLVM.AppendBasicBlock(func, "branch");
            LLVMBasicBlockRef mergeBB = LLVM.AppendBasicBlock(func, "forcont");

            // Build condition
            LLVM.BuildBr(_builder, condBB);
            LLVM.PositionBuilderAtEnd(_builder, condBB);
            LLVMValueRef condition = stmt.Condition.Accept(this);
            LLVM.BuildCondBr(_builder, condition, branchBB, mergeBB);

            // branch
            LLVM.PositionBuilderAtEnd(_builder, branchBB); // Position builder at block
            stmt.Branch.Accept(this); // Generate branch code
            LLVM.BuildBr(_builder, condBB);

            LLVM.PositionBuilderAtEnd(_builder, mergeBB);

            return null;
        }

        public object Visit(ForStmt stmt)
        {
            LLVMValueRef func = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(_builder));
            new VarDeclarationStmt(stmt.StartVal.DataType, stmt.VarName, new List<IExpression>() {}).Accept(this);

            // Blocks
            LLVMBasicBlockRef condBB = LLVM.AppendBasicBlock(func, "cond");
            LLVMBasicBlockRef branchBB = LLVM.AppendBasicBlock(func, "branch");
            LLVMBasicBlockRef mergeBB = LLVM.AppendBasicBlock(func, "forcont");

            // Build condition
            LLVM.BuildBr(_builder, condBB);
            LLVM.PositionBuilderAtEnd(_builder, condBB);
            LLVMValueRef counter = new VariableExpr(stmt.VarName).Accept(this);
            LLVMValueRef maxVal = stmt.MaxVal.Accept(this);
            LLVMValueRef incr = stmt.Increment == null
                                ? LLVM.ConstInt(stmt.StartVal.DataType.BaseType.ToLLVMType(), 1, LLVMBoolTrue)
                                : stmt.Increment.Accept(this);
            LLVMValueRef condition = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntSLT, counter, maxVal, "tmpcmp");

            LLVM.BuildCondBr(_builder, condition, branchBB, mergeBB);

            // branch
            LLVM.PositionBuilderAtEnd(_builder, branchBB); // Position builder at block
            stmt.Branch.Accept(this); // Generate branch code

            // Increment counter
            LLVMValueRef newVal = LLVM.BuildAdd(_builder, counter, incr, "tmpadd");
            LLVM.BuildStore(_builder, newVal, _namedValues[stmt.VarName.Lexeme].ValueRef);

            LLVM.BuildBr(_builder, condBB);

            LLVM.PositionBuilderAtEnd(_builder, mergeBB);

            return null;
        }

        public object Visit(ExpressionStmt stmt)
        {
            stmt.Expression.Accept(this);

            return null;
        }

        public LLVMValueRef Visit(BinaryExpr expr)
        {
            LLVMValueRef left = expr.Left.Accept(this);
            LLVMValueRef right = expr.Right.Accept(this);
            LLVMValueRef value = _llvmHelper.BuildBinary(left, expr.Operator.Type, right, expr.DataType);
            value = CastIfNeeded(value, expr.Cast);

            return value;
        }

        // This is a bit messy at the moment, sighs. Trying to decide what to do with it.
        public LLVMValueRef Visit(LiteralExpr expr)
        {
            string literal = (string)expr.Value.Literal;
            var baseType = expr.DataType.BaseType;
            LLVMValueRef llvmValue;

            if (baseType.IsInt() || baseType.IsBool())
            {
                ulong longValue = ulong.Parse(literal);
                var isSigned = longValue < 0 ? LLVMBoolFalse : LLVMBoolTrue;
                llvmValue = LLVM.ConstInt(baseType.ToLLVMType(), longValue, isSigned);
            }
            else if (baseType.IsFloat())
            {
                llvmValue = LLVM.ConstReal(baseType.ToLLVMType(), double.Parse(literal));
            }
            else
            {
                // Other datatypes, just string for now, but will probably become more? Hence the switch.
                switch (baseType)
                {
                    case BaseType.StringConst:
                        var stringConst = LLVM.ConstString(literal, (uint)literal.Length, LLVMBoolTrue);
                        llvmValue = LLVM.BuildGlobalString(_builder, literal, ".str");
                        break;
                    default:
                        throw new Exception($"Unknown datatype '{baseType.ToString()}'.");
                }
            }

            llvmValue = CastIfNeeded(llvmValue, expr.Cast);

            return llvmValue;
        }

        public LLVMValueRef Visit(VariableExpr expr)
        {
            NamedValue namedValue = _namedValues[expr.Name.Lexeme];
            LLVMValueRef valueRef = namedValue.ValueRef;

            if (expr.ArrayIndexes != null && expr.ArrayIndexes.Count > 0)
            {
                //var indices = new LLVMValueRef[] {
                valueRef = GetArrayItem(valueRef, namedValue.ArrayDepth, expr.ArrayIndexes);
                //};
                //valueRef = LLVM.BuildGEP(_builder, valueRef, indices, "geptmp");
            }

            if (!expr.IsArgumentVar)
            {
                valueRef = LLVM.BuildLoad(_builder, valueRef, "l" + expr.Name.Lexeme);
            }

            valueRef = CastIfNeeded(valueRef, expr.Cast);

            return valueRef;
        }

        public LLVMValueRef Visit(GroupExpr expr)
        {
            return expr.Expression.Accept(this);
        }

        public LLVMValueRef Visit(UnaryExpr expr)
        {
            LLVMValueRef value = expr.Expression.Accept(this);

            if (expr.Operator.Type == TokenType.Bang) // !expr
            {
                value = LLVM.BuildNot(_builder, value, "nottmp");
            }
            else
            {
                value = LLVM.BuildNeg(_builder, value, "negtmp"); // -expr
            }

            return value;
        }

        public LLVMValueRef Visit(CallExpr expr)
        {
            var callee = LLVM.GetNamedFunction(_module, expr.Name.Lexeme);
            int paramCount = expr.Parameters.Count;

            if (callee.Pointer == IntPtr.Zero)
            {
                Reporter.Error(expr.Name.Position, "Unkown function.");
            }

            var callParams = new LLVMValueRef[expr.Parameters.Count];
            for (int i = 0; i < paramCount; i++)
            {
                IExpression paramExpr = expr.Parameters[i];
                LLVMValueRef paramValue = paramExpr.Accept(this);
                callParams[i] = paramValue;

                if (expr.Parameters[i].Cast != BaseType.Unknown)
                {
                    var cast = LLVM.BuildIntCast(_builder, paramValue,
                            expr.Parameters[i].Cast.ToLLVMType(), "callcasttmp");

                    callParams[i] = CastIfNeeded(paramValue, expr.Parameters[i].Cast);
                }
            }

            LLVMValueRef call = LLVM.BuildCall(_builder, callee, callParams, "calltmp");
            if (expr.ArrayIndexes != null)
            {
                call = GetArrayItem(call, expr.DataType.ArrayDepth, expr.ArrayIndexes);
            }

            return call;
        }

        /// <summary>
        /// Get array item from indexes.
        /// </summary>
        private LLVMValueRef GetArrayItem(LLVMValueRef array, int arrayDepth, List<IExpression> arrayIndexes)
        {
            Stack<LLVMValueRef> arrayLengths = new Stack<LLVMValueRef>();
            for (int i = 0; i < arrayDepth; i++)
            {
                var indices = new LLVMValueRef[]
                {
                    LLVM.ConstInt(LLVM.Int32Type(), (ulong)i, LLVMBoolFalse)
                };
                LLVMValueRef gep = LLVM.BuildGEP(_builder, array, indices, "geptmp");
                LLVMValueRef length = LLVM.BuildLoad(_builder, gep, "loadtmp");
                arrayLengths.Push(length);
            }

            LLVMValueRef index = arrayIndexes[0].Accept(this);
            for (int i = 0; i <= arrayIndexes.Count - 1; i++)
            {
                LLVMValueRef currentIndex = arrayIndexes[i].Accept(this);
                LLVMValueRef size = arrayLengths.Pop();
                var mul = LLVM.BuildMul(_builder, currentIndex, size, "multmp"); // Multiply index with declared size on the opposite end

                index = LLVM.BuildAdd(_builder, index, mul, "addtmp");
            }

            LLVMValueRef arrayDepthConst = LLVM.ConstInt(LLVM.Int32Type(),
                                                         (ulong)arrayDepth,
                                                         LLVMBoolFalse);
            index = LLVM.BuildAdd(_builder, index, arrayDepthConst, "offsettmp"); // Offset by array depth, since array lengths are loaded in front of the other items

            return LLVM.BuildGEP(_builder, array, new LLVMValueRef[] { index }, "geptmp");
        }

        /// <summary>
        /// Return LLVMValueRef with cast if it is supposed to get one, otherwise just return the original value.
        /// </summary>
        private LLVMValueRef CastIfNeeded(LLVMValueRef value, BaseType cast)
        {
            if (cast != BaseType.Unknown)
            {
                LLVMTypeRef castAsLLVM = cast.ToLLVMType();
                if (cast == BaseType.String || cast.IsInt())
                {
                    return LLVM.BuildIntCast(_builder, value, castAsLLVM, "intCast");
                }
                else if (cast.IsFloat())
                {
                    return LLVM.BuildSIToFP(_builder, value, castAsLLVM, "sitofpCast");
                }
            }

            return value;
        }
    }
}
