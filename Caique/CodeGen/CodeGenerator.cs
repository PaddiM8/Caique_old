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
        private LLVMModuleRef _module { get; }
        private LLVMBuilderRef _builder { get; }
        private LLVMHelper _llvmHelper { get; }

        // LLVMBool
        private static readonly LLVMBool LLVMBoolFalse = new LLVMBool(0);
        private static readonly LLVMBool LLVMBoolTrue = new LLVMBool(1);

        // Data structures
        private readonly Dictionary<string, LLVMValueRef> _namedValues
            = new Dictionary<string, LLVMValueRef>();
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
            LLVMTypeRef type = stmt.DataType.ToLLVMType();

            // If not null or empty
            if (stmt.ArraySizes != null && stmt.ArraySizes.Count > 0)
            {
                foreach (var size in stmt.ArraySizes)
                {
                    //type = LLVM.Array(type, size.Accept(this));
                }
            }

            var alloca = LLVM.BuildAlloca(_builder, type, stmt.Identifier.Lexeme); // Allocate variable
            if (stmt.Value != null)
            {
                LLVMValueRef initializer = stmt.Value.Accept(this);
                LLVM.BuildStore(_builder, initializer, alloca);
            }

            _namedValues.Add(stmt.Identifier.Lexeme, alloca); // Add to dictionary

            return null;
        }

        public object Visit(AssignmentStmt stmt)
        {
            LLVMValueRef value = stmt.Value.Accept(this); // Get value after '='

            LLVMValueRef varRef;
            if (_namedValues.TryGetValue(stmt.Identifier.Lexeme, out varRef)) // Get variable reference
            {
                LLVM.BuildStore(_builder, value, varRef); // Build assignment
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
                arguments[i] = stmt.Arguments[i].Type.ToLLVMType();
            }

            LLVMTypeRef functionType = LLVM.FunctionType(stmt.ReturnType.ToLLVMType(), arguments, LLVMBoolFalse);
            LLVMValueRef function = LLVM.AddFunction(_module, stmt.Name.Lexeme, functionType);

            // Add names to arguments and allocate memory
            for (int k = 0; k < arguments.Length; k++)
            {
                string argumentName = stmt.Arguments[k].Name.Lexeme;
                LLVMValueRef param = LLVM.GetParam(function, (uint)k);
                LLVM.SetValueName(param, argumentName);

                _namedValues[argumentName] = param;
            }

            _valueStack.Push(new Tuple<LLVMValueRef, BlockStmt>(function, stmt.Block));
            //stmt.Block.Accept(this); // Code block after function

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
            var dataType = expr.DataType;
            LLVMValueRef llvmValue;

            if (dataType.IsInt() || dataType.IsBool())
            {
                ulong longValue = ulong.Parse(literal);
                var isSigned = longValue < 0 ? LLVMBoolFalse : LLVMBoolTrue;
                llvmValue = LLVM.ConstInt(dataType.ToLLVMType(), longValue, isSigned);
            }
            else if (dataType.IsFloat())
            {
                llvmValue = LLVM.ConstReal(dataType.ToLLVMType(), double.Parse(literal));
            }
            else
            {
                // Other datatypes, just string for now, but will probably become more? Hence the switch.
                switch (dataType)
                {
                    case DataType.StringConst:
                        var stringConst = LLVM.ConstString(literal, (uint)literal.Length, LLVMBoolTrue);
                        llvmValue = LLVM.BuildGlobalString(_builder, literal, ".str");
                        break;
                    default:
                        throw new Exception($"Unknown datatype '{dataType.ToString()}'.");
                }
            }

            llvmValue = CastIfNeeded(llvmValue, expr.Cast);

            return llvmValue;
        }

        public LLVMValueRef Visit(VariableExpr expr)
        {
            LLVMValueRef value = _namedValues[expr.Name.Lexeme];

            if (!expr.IsArgumentVar)
            {
                value = LLVM.BuildLoad(_builder, value, "l" + expr.Name.Lexeme);
            }

            value = CastIfNeeded(value, expr.Cast);

            return value;
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

                if (expr.Parameters[i].Cast != DataType.Unknown)
                {
                    var cast = LLVM.BuildIntCast(_builder, paramValue,
                            expr.Parameters[i].Cast.ToLLVMType(), "callcasttmp");

                    callParams[i] = CastIfNeeded(paramValue, expr.Parameters[i].Cast);
                }
            }

            return LLVM.BuildCall(_builder, callee, callParams, "calltmp");
        }

        /// <summary>
        /// Return LLVMValueRef with cast if it is supposed to get one, otherwise just return the original value.
        /// </summary>
        private LLVMValueRef CastIfNeeded(LLVMValueRef value, DataType cast)
        {
            if (cast != DataType.Unknown)
            {
                LLVMTypeRef castAsLLVM = cast.ToLLVMType();
                if (cast == DataType.String || cast.IsInt())
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
