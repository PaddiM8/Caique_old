using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class LiteralExpr : IExpression
    {
        public Token Value { get; }

        public LiteralExpr(Token value)
        {
            this.Value = value;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
