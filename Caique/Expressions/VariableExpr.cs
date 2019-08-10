using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class VariableExpr : IExpression
    {
        public Token Name { get; }

        public VariableExpr(Token name)
        {
            this.Name = name;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
