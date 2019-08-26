using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class GroupExpr : IExpression
    {
        public IExpression       Expression   { get; }
        public BaseType          Cast         { get; set; }
        public DataType          DataType     { get; set; }
        public List<IExpression> ArrayIndexes { get; }

        public GroupExpr(IExpression expression, List<IExpression> arrayIndexes)
        {
            this.Expression = expression;
            this.ArrayIndexes = arrayIndexes;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
