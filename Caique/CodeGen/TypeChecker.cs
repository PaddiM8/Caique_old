using System;
using System.Collections.Generic;
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

        // variableName/functionName, Tuple(DataType, IsArgumentVar)
        private Dictionary<string, Tuple<DataType, bool>> _types =
            new Dictionary<string, Tuple<DataType, bool>>();

        // First DataType is the return type, the rest are the arguments
        private Dictionary<string, DataType[]> _functions;
            /*new Dictionary<string, DataType[]>()
        {
            { "printf", new DataType[] { DataType.Void, DataType.String, DataType.Variadic } },
        };*/

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

        /// <summary>
        /// Check if two types are compatible and if one of them needs a cast.
        /// </summary>
        private TypeCompatibility Check(DataType type1, DataType type2)
        {
            if (type1 == type2 || type1 == DataType.Variadic) return TypeCompatibility.Compatible;
            if (CanBeCast(type1, type2)) return TypeCompatibility.NeedsCast;

            return TypeCompatibility.Incompatible;
        }

        /// <summary>
        /// Check if a DataType can be cast into another DataType.
        /// </summary>
        private bool CanBeCast(DataType type1, DataType type2)
        {
            if (type1 == DataType.StringConst &&
                type2 == DataType.String) return true;
            if (type1 == DataType.String &&
                type2 == DataType.StringConst) return true;
            if (type1 == DataType.Double &&
                type2 == DataType.Int) return true;

            return false;
        }

        /// <summary>
        /// Figures out the best cast depending on the two types and which one gets the cast
        /// </summary>
        /// <returns>Tuple with Item1 being a DataType of the cast, and Item2 being a bool that is true if the first type is supposed to get the cast</returns>
        private Tuple<DataType, bool> CreateCastingRule(DataType type1, DataType type2)
        {
            if (type1 == DataType.String && type2 == DataType.StringConst)
                return new Tuple<DataType, bool>(type1, false);
            if (type1 == DataType.Double && type2 == DataType.Int)
                return new Tuple<DataType, bool>(type1, false);

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

                // If cast should be applied leftExpr
                if (castInfo.Item2) leftExpr.Cast  = castInfo.Item1;
                else                rightExpr.Cast = castInfo.Item1;

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

        // TODO: Make sure both sides match
        public object Visit(VarDeclarationStmt stmt)
        {
            if (stmt.Value != null)
            {
                DataType type1 = stmt.DataType;
                DataType type2 = stmt.Value.Accept(this);

                DataType finalType = ApplyCastingRuleIfNeeded(stmt.Identifier.Position, type2, type1, stmt.Value);
                _types[stmt.Identifier.Lexeme] =
                    new Tuple<DataType, bool>(finalType, false);
            }
            else
            {
                _types[stmt.Identifier.Lexeme] =
                    new Tuple<DataType, bool>(stmt.DataType, false);
            }

            return null;
        }

        public object Visit(AssignmentStmt stmt)
        {
            stmt.Identifier.DataType = _types[stmt.Identifier.Lexeme].Item1;
            DataType type1 = stmt.Identifier.DataType;
            DataType type2 = stmt.Value.Accept(this);

            ApplyCastingRuleIfNeeded(stmt.Identifier.Position, type2, type1, stmt.Value);

            return null;
        }

        public object Visit(FunctionStmt stmt)
        {
            _currentFunctionType = stmt.ReturnType;

            //DataType[] argumentTypes = new DataType[stmt.Arguments.Count+1];
            //argumentTypes[0] = stmt.ReturnType;

            for (int i = 0; i < stmt.Arguments.Count; i++)
            {
                Argument argument = stmt.Arguments[i];
                _types[argument.Name.Lexeme] = new Tuple<DataType, bool>(argument.Type, true);
                //argumentTypes[i+1] = stmt.Arguments[i].Type;
            }

            //_functions[stmt.Name.Lexeme] = argumentTypes;
            stmt.Block.Accept(this);
            return null;
        }

        public object Visit(BlockStmt stmt)
        {
            foreach (var statement in stmt.Statements) statement.Accept(this);
            return null;
        }

        public object Visit(ReturnStmt stmt)
        {
            DataType type2 = stmt.Expression.Accept(this);
            ApplyCastingRuleIfNeeded(new Pos(0, 0), type2, _currentFunctionType, stmt.Expression);

            return null;
        }

        public DataType Visit(BinaryExpr expr)
        {
            DataType type1 = expr.Left.Accept(this);
            DataType type2 = expr.Right.Accept(this);

            return ApplyCastingRuleIfNeeded(expr.Operator.Position, type2, type1, expr.Right, expr.Left);
        }

        public DataType Visit(LiteralExpr expr)
        {
            return expr.Value.DataType;
        }

        public DataType Visit(VariableExpr expr)
        {
            Tuple<DataType, bool> info = _types[expr.Name.Lexeme];
            expr.Name.DataType = info.Item1;
            expr.IsArgumentVar = info.Item2;
            return expr.Name.DataType;
        }

        public DataType Visit(CallExpr expr)
        {
            DataType[] functionTypes = _functions[expr.Name.Lexeme];
            expr.Name.DataType = functionTypes[0];

            // If the parameters passed aren't the same amount as arguments expected, and the last DataType isn't variadic(meaning any amount could be passed)
            if (functionTypes.Length - 1 != expr.Parameters.Count &&
                functionTypes[functionTypes.Length - 1] != DataType.Variadic)
            {
                Reporter.Error(expr.Name.Position, "Incorrect amount of parameters passed.");
                return expr.Name.DataType;
            }

            for (int i = 0; i < expr.Parameters.Count; i++)
            {
                DataType paramDataType = expr.Parameters[i].Accept(this);
                ApplyCastingRuleIfNeeded(expr.Name.Position, paramDataType,
                                         functionTypes[i+1], expr.Parameters[i]);
            }

            return expr.Name.DataType;
        }

        public DataType Visit(GroupExpr expr)
        {
            return expr.Expression.Accept(this);
        }
    }
}
