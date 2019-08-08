using System;
using System.Collections.Generic;

namespace Caique.Expressions
{
    class GroupExpr : IExpression
    {
        public IExpression Expression { get; }

        public GroupExpr(IExpression expression)
        {
            this.Expression = expression;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
