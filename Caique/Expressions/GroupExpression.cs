using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class GroupExpr : IExpression
    {
        public IExpression Expression { get; }
        public DataType    Cast       { get; set; }
        public DataType    DataType   { get; set; }

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
