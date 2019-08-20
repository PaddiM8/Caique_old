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

    class TypeChecker : IExpressionVisitor<BaseType>, IStatementVisitor<object>
    {
        private List<IStatement> _statements { get; }
        private BaseType _currentFunctionType;
        private List<CallExpr> _callExpressions = new List<CallExpr>();
        private Scope _scope = new Scope();

        // variableName/functionName, Tuple(BaseType, IsArgumentVar)
        private Dictionary<string, Tuple<BaseType, bool>> _types =
            new Dictionary<string, Tuple<BaseType, bool>>();

        // First BaseType is the return type, the rest are the arguments
        private Dictionary<string, BaseType[]> _functions;

        public TypeChecker(List<IStatement> statements, Dictionary<string, BaseType[]> functions)
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

        /// <summary>
        /// Check if two types are compatible and if one of them needs a cast.
        /// </summary>
        private TypeCompatibility Check(BaseType type1, BaseType type2)
        {
            if (type1 == type2 || type1 == BaseType.Variadic) return TypeCompatibility.Compatible;
            if (CanBeCast(type1, type2)) return TypeCompatibility.NeedsCast;

            return TypeCompatibility.Incompatible;
        }

        /// <summary>
        /// Check if a BaseType can be cast into another BaseType.
        /// </summary>
        private bool CanBeCast(BaseType type1, BaseType type2)
        {
            //  String, StringConst
            if (type1 == BaseType.StringConst &&
                type2 == BaseType.String) return true;
            if (type1 == BaseType.String &&
                type2 == BaseType.StringConst) return true;

            if (type1.IsNumber() && type2.IsNumber()) return true;

            return false;
        }

        /// <summary>
        /// Figures out the best cast depending on the two types and which one gets the cast
        /// </summary>
        /// <returns>Tuple with Item1 being a BaseType of the cast, and Item2 being a bool that is true if the first type is supposed to get the cast</returns>
        private Tuple<BaseType, bool> CreateCastingRule(DetaType type1, BaseType type2)
        {
            // String, StringConst
            if (type1 == BaseType.String && type2 == BaseType.StringConst)
                return new Tuple<BaseType, bool>(type1, false);
            if (type1 == BaseType.StringConst && type2 == BaseType.String)
                return new Tuple<BaseType, bool>(type2, true);

            if (type1.IsNumber() && type2.IsNumber())
            {
                // Cast the lower worth one to the higher worth one. Any int is lower than float, double is higher than float, etc.
                if (type1 > type2) return new Tuple<BaseType, bool>(type1, false);
                else               return new Tuple<BaseType, bool>(type2, true);
            }

            throw new Exception("Couldn't cast");
        }

        /// <summary>
        /// Apply casting rule to expression if it needs one, depending on surrounding types.
        /// </summary>
        private BaseType ApplyCastingRuleIfNeeded(Pos pos, BaseType rightType, BaseType leftType, IExpression rightExpr, IExpression leftExpr = null)
        {
            TypeCompatibility compatibility = Check(leftType, rightType);
            if (compatibility == TypeCompatibility.Compatible)
            {
                return leftType;
            }
            else if (compatibility == TypeCompatibility.NeedsCast)
            {
                Tuple<BaseType, bool> castInfo = CreateCastingRule(leftType, rightType);

                // If leftExpr isn't specified, throw exception if it is the one to be casted.
                if (leftExpr == null && castInfo.Item2)
                {
                    Reporter.Error(pos, $"Invalid type combination {rightType.ToString()}, {leftType.ToString()}.");
                    return leftType;
                }

                // If cast should be applied leftExpr
                if (castInfo.Item2)
                {
                    if (leftExpr is LiteralExpr && leftType.IsNumber()) leftExpr.BaseType = castInfo.Item1;
                    else leftExpr.Cast = castInfo.Item1;
                }
                else
                {
                    if (rightExpr is LiteralExpr && leftType.IsNumber()) rightExpr.BaseType = castInfo.Item1;
                    else rightExpr.Cast = castInfo.Item1;
                }

                return castInfo.Item1;
            }

            Reporter.Error(pos, $"Invalid type combination {leftType.ToString()}, {rightType.ToString()}");
            return leftType;
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
                    if (!size.Accept(this).IsInt())
                        Reporter.Error(new Pos(0, 0), "Array size specifier should be of type 'int'.");
                }
            }

            // If assignment is present
            if (stmt.Value != null)
            {
                BaseType type1 = stmt.BaseType;
                BaseType type2 = stmt.Value.Accept(this);

                // Make sure the assignment value is compatible with the variable type.
                BaseType finalType = ApplyCastingRuleIfNeeded(stmt.Identifier.Position, type2, type1, stmt.Value);
                //_types[stmt.Identifier.Lexeme] =
                _scope.Define(stmt.Identifier.Lexeme, finalType, false);
            }
            else
            {
                //_types[stmt.Identifier.Lexeme] =
                _scope.Define(stmt.Identifier.Lexeme, stmt.BaseType, false);
            }

            return null;
        }

        public object Visit(AssignmentStmt stmt)
        {
            stmt.Identifier.BaseType = _scope.Get(stmt.Identifier.Lexeme).Item1;
            BaseType type1 = stmt.Identifier.BaseType;
            BaseType type2 = stmt.Value.Accept(this);

            ApplyCastingRuleIfNeeded(stmt.Identifier.Position, type2, type1, stmt.Value);

            return null;
        }

        public object Visit(FunctionStmt stmt)
        {
            _currentFunctionType = stmt.ReturnType;

            for (int i = 0; i < stmt.Arguments.Count; i++)
            {
                Argument argument = stmt.Arguments[i];
                _scope.Define(argument.Name.Lexeme, argument.Type, true);
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
            BaseType type2 = stmt.Expression.Accept(this);
            ApplyCastingRuleIfNeeded(new Pos(0, 0), type2, _currentFunctionType, stmt.Expression);

            return null;
        }

        public object Visit(IfStmt stmt)
        {
            if (stmt.Condition.Accept(this) != BaseType.Boolean)
            {
                Reporter.Error(new Pos(0, 0), "Expected type 'bool' as conditional.");
            }

            stmt.ThenBranch.Accept(this);
            if (stmt.ElseBranch != null) stmt.ElseBranch.Accept(this);

            return null;
        }

        public BaseType Visit(BinaryExpr expr)
        {
            BaseType type1 = expr.Left.Accept(this);
            BaseType type2 = expr.Right.Accept(this);

            BaseType finalType = ApplyCastingRuleIfNeeded(expr.Operator.Position,
                                                          type2,
                                                          type1,
                                                          expr.Right,
                                                          expr.Left);
            // Comparison expressions are of type boolean
            if (expr.Operator.Type.IsComparisonOperator())
            {
                expr.BaseType = BaseType.Boolean;
            }
            else // Arithmetic operators are of the operands' type.
            {
                expr.BaseType = finalType;
            }

            return expr.BaseType;
        }

        public BaseType Visit(LiteralExpr expr)
        {
            expr.BaseType = expr.Value.BaseType;
            return expr.Value.BaseType;
        }

        public BaseType Visit(VariableExpr expr)
        {
            //Tuple<BaseType, bool> info = _types[expr.Name.Lexeme];

            Tuple<BaseType, bool> info = _scope.Get(expr.Name.Lexeme);
            expr.Name.BaseType = info.Item1;
            expr.IsArgumentVar = info.Item2;

            expr.BaseType = expr.Name.BaseType;
            return expr.Name.BaseType;
        }

        public BaseType Visit(UnaryExpr expr)
        {
            BaseType exprBaseType = expr.Expression.Accept(this);

            if (expr.Operator.Type == TokenType.Bang) // !expr
            {
                if (exprBaseType != BaseType.Int1 && exprBaseType != BaseType.Boolean)
                {
                    Reporter.Error(expr.Operator.Position, "Expected type 'bool' or 'i1' after '!'.");
                }
            }
            else if (expr.Operator.Type == TokenType.Minus) // -expr
            {
                if (!exprBaseType.IsNumber())
                {
                    Reporter.Error(expr.Operator.Position, "Expected number after '-'.");
                }
            }

            return exprBaseType;
        }

        public BaseType Visit(CallExpr expr)
        {
            BaseType[] functionTypes = _functions[expr.Name.Lexeme];
            expr.Name.BaseType = functionTypes[0];

            // If the parameters passed aren't the same amount as arguments expected, and the last BaseType isn't variadic(meaning any amount could be passed)
            if (functionTypes.Length - 1 != expr.Parameters.Count &&
                functionTypes[functionTypes.Length - 1] != BaseType.Variadic)
            {
                Reporter.Error(expr.Name.Position, "Incorrect amount of parameters passed.");
                expr.BaseType = expr.Name.BaseType;
                return expr.Name.BaseType;
            }

            for (int i = 0; i < expr.Parameters.Count; i++)
            {
                BaseType paramBaseType = expr.Parameters[i].Accept(this);
                ApplyCastingRuleIfNeeded(expr.Name.Position, paramBaseType,
                                         functionTypes[i+1], expr.Parameters[i]);
            }

            expr.BaseType = expr.Name.BaseType;
            return expr.Name.BaseType;
        }

        public BaseType Visit(GroupExpr expr)
        {
            BaseType type = expr.Expression.Accept(this);

            expr.BaseType = type;
            return type;
        }
    }
}
