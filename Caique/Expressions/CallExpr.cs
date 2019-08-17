using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class CallExpr : IExpression
    {
        public Token             Name       { get; }
        public List<IExpression> Parameters { get; }
        public DataType          Cast       { get; set; }
        public DataType          DataType   { get; set; }

        public CallExpr(Token name, List<IExpression> parameters)
        {
            this.Name = name;
            this.Parameters = parameters;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
