using System;
using System.Collections.Generic;
using LLVMSharp;
using Caique.Models;
using Caique.Expressions;
using Caique.Statements;
using Caique.Logging;

namespace Caique.CodeGen
{
    class CodeGenerator : IExpressionVisitor<LLVMValueRef>, IStatementVisitor<object>
    {

        private readonly LLVMModuleRef _module;
        private readonly LLVMBuilderRef _builder;
        private static readonly LLVMBool LLVMBoolFalse = new LLVMBool(0);
        private static readonly LLVMBool LLVMBoolTrue = new LLVMBool(1);
        private readonly Dictionary<string, LLVMValueRef> _namedValues = new Dictionary<string, LLVMValueRef>();
        private readonly Stack<LLVMValueRef> _valueStack = new Stack<LLVMValueRef>();
        private readonly List<IStatement> _statements;
        //private readonly ScopeEnv _environment = new ScopeEnv();

        public CodeGenerator(List<IStatement> statements)
        {
            this._statements = statements;

            _module = LLVM.ModuleCreateWithName("main");
            _builder = LLVM.CreateBuilder();

            LLVMTypeRef stringType = LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0);
            //var functype = LLVM.FunctionType(LLVM.Int32Type(), new LLVMTypeRef[] { }, false);
            //var main = LLVM.AddFunction(_module, "main", functype);
            //var entrypoint = LLVM.AppendBasicBlock(main, "entrypoint");
            //LLVM.PositionBuilderAtEnd(_builder, entrypoint);

            // String
            //LLVMValueRef output = LLVM.AddGlobal(_module, LLVMTypeRef.ArrayType(LLVMTypeRef.Int8Type(), 4), ".str");
            //var constString = LLVM.ConstString("%f\n", 3, false);
            //output.SetInitializer(constString);

            // printf
            //var cast = LLVM.BuildIntCast(_builder, output, LLVM.PointerType(LLVMTypeRef.Int8Type(), 0), "tmpcast");
            var printfArguments = new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0) };
            var printf = LLVM.AddFunction(_module, "printf", LLVM.FunctionType(LLVMTypeRef.Int32Type(), printfArguments, LLVMBoolTrue));
            LLVM.SetLinkage(printf, LLVMLinkage.LLVMExternalLinkage);

            for (int i = 0; i < _statements.Count; i++)
            {
                _statements[i].Accept(this);
            }

            //LLVM.BuildCall(_builder, printf, new LLVMValueRef[] { cast, _valueStack.Pop() }, "calltmp");

            //LLVM.BuildRet(_builder, LLVM.ConstInt(LLVM.Int32Type(), 0, false));
            string error;
            LLVM.DumpModule(_module);
            LLVM.PrintModuleToFile(_module, "test.ll", out error);
            Console.WriteLine();
        }

        public object Visit(VarDeclarationStmt stmt)
        {
            LLVMValueRef initializer = stmt.Value.Accept(this);
            LLVMTypeRef type = stmt.DataType.ToLLVMType();

            var variable = LLVM.BuildAlloca(_builder, type, stmt.Identifier.Lexeme); // Allocate variable
            variable.SetInitializer(initializer); // Set initial value

            _namedValues.Add(stmt.Identifier.Lexeme, variable); // Add to dictionary

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

            // Add names to arguments
            for (int k = 0; k < arguments.Length; k++)
            {
                string argumentName = stmt.Arguments[k].Name.Lexeme;
                LLVMValueRef param = LLVM.GetParam(function, (uint)k);
                LLVM.SetValueName(param, argumentName);

                _namedValues[argumentName] = param;
            }

            _valueStack.Push(function);
            stmt.Block.Accept(this); // Code block after function

            return null;
        }

        public object Visit(BlockStmt stmt)
        {
            LLVM.PositionBuilderAtEnd(_builder, LLVM.AppendBasicBlock(_valueStack.Pop(), "entry"));
            foreach (IStatement subStmt in stmt.Statements)
            {
                subStmt.Accept(this);
            }

            return null;
        }

        public object Visit(ReturnStmt stmt)
        {
            LLVMValueRef exprValue = stmt.Expression.Accept(this);
            LLVM.BuildRet(_builder, exprValue);

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

            LLVMValueRef result;

            switch (expr.Operator.Type)
            {
                case TokenType.Plus:
                    result = LLVM.BuildFAdd(_builder, left, right, "addtmp");
                    break;
                case TokenType.Minus:
                    result = LLVM.BuildFSub(_builder, left, right, "subtmp");
                    break;
                case TokenType.Star:
                    result = LLVM.BuildFMul(_builder, left, right, "multmp");
                    break;
                case TokenType.Slash:
                    result = LLVM.BuildFDiv(_builder, left, right, "divtmp");
                    break;
                default: throw new Exception("Invalid binary operator."); // Invalid code should not have come here in the first place.
            }

            return result;
        }

        public LLVMValueRef Visit(LiteralExpr expr)
        {
            object literal = expr.Value.Literal;
            LLVMTypeRef llvmType = expr.Value.DataType.ToLLVMType();
            LLVMValueRef llvmValue;

            // Temporary structure
            switch (expr.Value.DataType)
            {
                case DataType.String:
                    string val = (string)literal;
                    var stringConst = LLVM.ConstString(val, (uint)val.Length, LLVMBoolTrue);
                    llvmValue = LLVM.BuildGlobalString(_builder, val, ".str");
                    break;
                case DataType.Double:
                    llvmValue = LLVM.ConstReal(llvmType, (double)literal);
                    break;
                case DataType.Int:
                    llvmValue = LLVM.ConstInt(llvmType, (ulong)(int)literal, LLVMBoolTrue); // Optimize this!
                    break;
                case DataType.Boolean:
                    uint boolVal = (bool)literal ? 1u : 0u;
                    llvmValue = LLVM.ConstInt(llvmType, boolVal, LLVMBoolTrue);
                    break;
                default:
                    throw new Exception("Unknown datatype.");
            }

            return llvmValue;
        }

        public LLVMValueRef Visit(VariableExpr expr)
        {
            return _namedValues[expr.Name.Lexeme];
        }

        public LLVMValueRef Visit(GroupExpr expr)
        {
            return expr.Expression.Accept(this);
        }

        public LLVMValueRef Visit(CallExpr expr)
        {
            var callee = LLVM.GetNamedFunction(_module, expr.Name.Lexeme);
            int paramCount = expr.Parameters.Count;

            if (callee.Pointer == IntPtr.Zero)
            {
                Reporter.Error(expr.Name.Position, "Unkown function.");
            }

            if (LLVM.CountParams(callee) != paramCount)
            {
                Reporter.Error(expr.Name.Position, "Incorrect number of parameters passed.");
            }

            var callParams = new LLVMValueRef[expr.Parameters.Count];
            for (int i = 0; i < paramCount; i++)
            {
                IExpression paramExpr = expr.Parameters[i];
                LLVMValueRef paramValue = paramExpr.Accept(this);

                // Cast constant to i8* if needed
                if (paramExpr is LiteralExpr)
                {
                    if (((LiteralExpr)paramExpr).Value.DataType == DataType.String)
                    {
                        var cast = LLVM.BuildIntCast(_builder, paramValue,
                                LLVM.PointerType(LLVMTypeRef.Int8Type(), 0), "tmpcast");
                        callParams[i] = cast;
                    }
                }
                else
                {
                    callParams[i] = paramValue;
                }
            }

            return LLVM.BuildCall(_builder, callee, callParams, "calltmp");
        }
    }
}
