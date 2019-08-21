using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class VariableExpr : IExpression
    {
        public Token             Name          { get; }
        public BaseType          Cast          { get; set; }
        public DataType          DataType      { get; set; }
        public bool              IsArgumentVar { get; set; }
        public List<IExpression> ArrayIndexes  { get; }

        public VariableExpr(Token name, List<IExpression> arrayIndexes = null)
        {
            this.Name = name;
            this.ArrayIndexes = arrayIndexes;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
