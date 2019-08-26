using System;
using System.Collections.Generic;
using System.Linq;
using Caique.Models;
using Caique.Expressions;
using Caique.Statements;
using Caique.Logging;

namespace Caique.CodeGen
{
    enum TypeCompatibility
    {
        Compatible,
        NeedsCast,
        Incompatible,
    }

    class TypeChecker : IExpressionVisitor<DataType>, IStatementVisitor<object>
    {
        private List<IStatement> _statements { get; }
        private DataType _currentFunctionType;
        private List<CallExpr> _callExpressions = new List<CallExpr>();
        private Scope _scope = new Scope();

        // variableName/functionName, Tuple(BaseType, IsArgumentVar)
        private Dictionary<string, Tuple<DataType, bool>> _types =
            new Dictionary<string, Tuple<DataType, bool>>();

        // First BaseType is the return type, the rest are the arguments
        private Dictionary<string, DataType[]> _functions;

        public TypeChecker(List<IStatement> statements, Dictionary<string, DataType[]> functions)
        {
            this._statements = statements;
            this._functions = functions;
        }

        public void CheckTypes()
        {
            foreach (var statement in _statements)
            {
                statement.Accept(this);
            }
        }

        public object Visit(ExpressionStmt stmt)
        {
            stmt.Expression.Accept(this);
            return null;
        }

        public object Visit(VarDeclarationStmt stmt)
        {
            // If not null or empty
            if (stmt.ArraySizes != null && stmt.ArraySizes.Count > 0)
            {
                // Make sure each size specifier is of type int.
                foreach (var size in stmt.ArraySizes)
                {
                    if (!size.Accept(this).BaseType.IsInt())
                        Reporter.Error(new Pos(0, 0), "Array size specifier should be of type 'int'.");
                }
            }

            // If assignment is present
            if (stmt.Value != null)
            {
                DataType type1 = stmt.DataType;
                DataType type2 = stmt.Value.Accept(this);

                // Make sure the assignment value is compatible with the variable type.
                DataType finalType = ApplyCastingRuleIfNeeded(stmt.Identifier.Position, type2, type1, stmt.Value);
                //_types[stmt.Identifier.Lexeme] =
                _scope.Define(stmt.Identifier.Lexeme, finalType, false);
            }
            else
            {
                //_types[stmt.Identifier.Lexeme] =
                _scope.Define(stmt.Identifier.Lexeme, stmt.DataType, false);
            }

            return null;
        }

        public object Visit(AssignmentStmt stmt)
        {
            stmt.Identifier.DataType = _scope.Get(stmt.Identifier.Lexeme).Item1;
            DataType type1 = stmt.Identifier.DataType;
            DataType type2 = stmt.Value.Accept(this);

            foreach (IExpression arrIndex in stmt.ArrayIndexes)
            {
                arrIndex.Accept(this);
            }

            type1.ArrayDepth = type1.ArrayDepth - stmt.ArrayIndexes.Count; // Adjust depth after accounting for indexing.

            ApplyCastingRuleIfNeeded(stmt.Identifier.Position, type2, type1, stmt.Value);

            return null;
        }

        public object Visit(FunctionStmt stmt)
        {
            _currentFunctionType = stmt.ReturnType;

            for (int i = 0; i < stmt.Arguments.Count; i++)
            {
                Argument argument = stmt.Arguments[i];
                _scope.Define(argument.Name.Lexeme, argument.DataType, true);
            }

            stmt.Block.Accept(this);
            return null;
        }

        public object Visit(BlockStmt stmt)
        {
            _scope = _scope.AddChildScope();
            foreach (var statement in stmt.Statements) statement.Accept(this);
            _scope = _scope.Parent;

            return null;
        }

        public object Visit(ReturnStmt stmt)
        {
            DataType type2 = stmt.Expression.Accept(this);
            ApplyCastingRuleIfNeeded(new Pos(0, 0), type2, _currentFunctionType, stmt.Expression);

            return null;
        }

        public object Visit(IfStmt stmt)
        {
            DataType conditionType = stmt.Condition.Accept(this);
            if (conditionType.BaseType != BaseType.Boolean || conditionType.ArrayDepth > 0)
            {
                Reporter.Error(new Pos(0, 0), "Expected type 'bool' as conditional.");
            }

            stmt.ThenBranch.Accept(this);
            if (stmt.ElseBranch != null) stmt.ElseBranch.Accept(this);

            return null;
        }

        public DataType Visit(BinaryExpr expr)
        {
            DataType type1 = expr.Left.Accept(this);
            DataType type2 = expr.Right.Accept(this);

            DataType finalType = ApplyCastingRuleIfNeeded(expr.Operator.Position,
                                                          type2,
                                                          type1,
                                                          expr.Right,
                                                          expr.Left);
            // Comparison expressions are of type boolean
            if (expr.Operator.Type.IsComparisonOperator())
            {
                expr.DataType = new DataType(BaseType.Boolean, 0);
            }
            else // Arithmetic operators are of the operands' type.
            {
                expr.DataType = finalType;
            }

            return expr.DataType;
        }

        public DataType Visit(LiteralExpr expr)
        {
            expr.DataType = expr.Value.DataType;

            return ArrayIndexesOrDefault(expr.ArrayIndexes, expr.DataType);
        }

        public DataType Visit(VariableExpr expr)
        {
            Tuple<DataType, bool> info = _scope.Get(expr.Name.Lexeme);
            DataType dataType = info.Item1;

            if (expr.ArrayIndexes != null)
                foreach (IExpression arrIndex in expr.ArrayIndexes)
                {
                    arrIndex.Accept(this);
                }

            //dataType.ArrayDepth = dataType.ArrayDepth - expr.ArrayIndexes.Count; // Adjust depth after accounting for indexing.
            expr.Name.DataType = dataType;
            expr.IsArgumentVar = info.Item2;

            expr.DataType = dataType;

            return ArrayIndexesOrDefault(expr.ArrayIndexes, dataType);
        }

        public DataType Visit(UnaryExpr expr)
        {
            DataType exprDataType = expr.Expression.Accept(this);

            if (expr.Operator.Type == TokenType.Bang) // !expr
            {
                if (exprDataType.BaseType != BaseType.Int1 && exprDataType.BaseType != BaseType.Boolean)
                {
                    Reporter.Error(expr.Operator.Position, "Expected type 'bool' or 'i1' after '!'.");
                }
            }
            else if (expr.Operator.Type == TokenType.Minus) // -expr
            {
                if (!exprDataType.BaseType.IsNumber())
                {
                    Reporter.Error(expr.Operator.Position, "Expected number after '-'.");
                }
            }

            return exprDataType;
        }

        public DataType Visit(CallExpr expr)
        {
            DataType[] functionTypes = _functions[expr.Name.Lexeme];
            bool isVariadic = functionTypes[functionTypes.Length - 1].BaseType == BaseType.Variadic;
            expr.Name.DataType = functionTypes[0];

            // If the parameters passed aren't the same amount as arguments expected, and the last BaseType isn't variadic(meaning any amount could be passed)
            if (functionTypes.Length - 1 != expr.Parameters.Count && !isVariadic)
            {
                Reporter.Error(expr.Name.Position, "Incorrect amount of parameters passed.");
                expr.DataType = expr.Name.DataType;
                return expr.Name.DataType;
            }

            for (int i = 0; i < expr.Parameters.Count; i++)
            {
                DataType argumentType = i >= functionTypes.Length - 1 && isVariadic // If at variadic arguments outside of the bounds of the functionTypes array
                                        ? functionTypes[functionTypes.Length - 1]   // Then the type is variadic
                                        : functionTypes[i + 1];                     // Else get the type normally

                DataType paramDataType = expr.Parameters[i].Accept(this);
                ApplyCastingRuleIfNeeded(expr.Name.Position, paramDataType,
                                         argumentType, expr.Parameters[i]);
            }

            expr.DataType = expr.Name.DataType;

            return ArrayIndexesOrDefault(expr.ArrayIndexes, expr.DataType);
        }

        public DataType Visit(GroupExpr expr)
        {
            DataType type = expr.Expression.Accept(this);
            expr.DataType = type;

            return ArrayIndexesOrDefault(expr.ArrayIndexes, type);
        }

        private DataType ArrayIndexesOrDefault(List<IExpression> arrayIndexes, DataType exprDataType)
        {
            if (arrayIndexes == null) return exprDataType;

            int rest = exprDataType.ArrayDepth - arrayIndexes.Count;
            exprDataType.ArrayDepth = rest;

            return exprDataType;
        }

        /// <summary>
        /// Check if two types are compatible and if one of them needs a cast.
        /// </summary>
        private TypeCompatibility Check(DataType type1, DataType type2)
        {
            if (type1.BaseType == type2.BaseType && type1.ArrayDepth == type2.ArrayDepth) return TypeCompatibility.Compatible;
            if (type1.BaseType == BaseType.Variadic) return TypeCompatibility.Compatible;
            if (CanBeCast(type1, type2)) return TypeCompatibility.NeedsCast;

            return TypeCompatibility.Incompatible;
        }

        /// <summary>
        /// Check if a BaseType can be cast into another BaseType.
        /// </summary>
        private bool CanBeCast(DataType type1, DataType type2)
        {
            //  String, StringConst
            if (type1.BaseType == BaseType.StringConst &&
                type2.BaseType == BaseType.String) return true;
            if (type1.BaseType == BaseType.String &&
                type2.BaseType == BaseType.StringConst) return true;

            if (type1.BaseType.IsNumber() && type2.BaseType.IsNumber()) return true;

            return false;
        }

        /// <summary>
        /// Figures out the best cast depending on the two types and which one gets the cast
        /// </summary>
        /// <returns>Tuple with Item1 being a BaseType of the cast, and Item2 being a bool that is true if the first type is supposed to get the cast</returns>
        private Tuple<DataType, bool> CreateCastingRule(DataType type1, DataType type2)
        {
            BaseType baseType1 = type1.BaseType;
            BaseType baseType2 = type2.BaseType;

            // String, StringConst
            if (baseType1 == BaseType.String && baseType2 == BaseType.StringConst)
                return new Tuple<DataType, bool>(type1, false);
            if (baseType1 == BaseType.StringConst && baseType2 == BaseType.String)
                return new Tuple<DataType, bool>(type2, true);

            if (baseType1.IsNumber() && baseType2.IsNumber())
            {
                // Cast the lower worth one to the higher worth one. Any int is lower than float, double is higher than float, etc.
                if (baseType1 > baseType2) return new Tuple<DataType, bool>(type1, false);
                else                       return new Tuple<DataType, bool>(type2, true);
            }

            throw new Exception("Couldn't cast");
        }

        /// <summary>
        /// Apply casting rule to expression if it needs one, depending on surrounding types.
        /// </summary>
        private DataType ApplyCastingRuleIfNeeded(Pos pos, DataType rightType, DataType leftType, IExpression rightExpr, IExpression leftExpr = null)
        {
            TypeCompatibility compatibility = Check(leftType, rightType);
            if (compatibility == TypeCompatibility.Compatible)
            {
                return leftType;
            }
            else if (compatibility == TypeCompatibility.NeedsCast)
            {
                Tuple<DataType, bool> castInfo = CreateCastingRule(leftType, rightType);

                // If leftExpr isn't specified, throw exception if it is the one to be casted.
                if (leftExpr == null && castInfo.Item2)
                {
                    Reporter.Error(pos, $"Invalid type combination {rightType.ToString()}, {leftType.ToString()}.");
                    return leftType;
                }

                // If cast should be applied leftExpr (make this neater, with a reference)
                if (castInfo.Item2)
                {
                    if (leftExpr is LiteralExpr && leftType.BaseType.IsNumber()) leftExpr.DataType = castInfo.Item1;
                    else leftExpr.Cast = castInfo.Item1.BaseType;
                }
                else
                {
                    if (rightExpr is LiteralExpr && leftType.BaseType.IsNumber()) rightExpr.DataType = castInfo.Item1;
                    else rightExpr.Cast = castInfo.Item1.BaseType;
                }

                return castInfo.Item1;
            }

            Reporter.Error(pos, $"Invalid type combination {leftType.ToString()}, {rightType.ToString()}");
            return leftType;
        }
    }
}
