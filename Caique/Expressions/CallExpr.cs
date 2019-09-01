using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class CallExpr : IExpression
    {
        public Token             Name         { get; }
        public List<IExpression> Parameters   { get; }
        public BaseType          Cast         { get; set; }
        public DataType          DataType     { get; set; }
        public List<IExpression> ArrayIndexes { get; }

        public CallExpr(Token name, List<IExpression> parameters, List<IExpression> arrayIndexes)
        {
            Name = name;
            Parameters = parameters;
            ArrayIndexes = arrayIndexes;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
