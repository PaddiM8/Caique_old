using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class GroupExpr : IExpression
    {
        public IExpression Expression { get; }
        public BaseType    Cast       { get; set; }
        public BaseType    BaseType   { get; set; }

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
