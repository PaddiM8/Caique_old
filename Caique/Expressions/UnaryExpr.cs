using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class UnaryExpr : IExpression
    {
        public Token       Operator   { get; }
        public IExpression Expression { get; }
        public BaseType    Cast       { get; set; }
        public DataType    DataType   { get; set; }

        public UnaryExpr(Token op, IExpression expression)
        {
            Operator = op;
            Expression = expression;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
