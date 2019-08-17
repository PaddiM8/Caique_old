using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class VariableExpr : IExpression
    {
        public Token    Name          { get; }
        public DataType Cast          { get; set; }
        public DataType DataType      { get; set; }
        public bool     IsArgumentVar { get; set; }

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
