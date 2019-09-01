using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class LiteralExpr : IExpression
    {
        public Token             Value        { get; }
        public BaseType          Cast         { get; set; }
        public DataType          DataType     { get; set; }
        public List<IExpression> ArrayIndexes { get; }

        public LiteralExpr(Token value, List<IExpression> arrayIndexes = null)
        {
            Value = value;
            ArrayIndexes = arrayIndexes;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
