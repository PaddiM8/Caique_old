using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class BinaryExpr : IExpression
    {
        public IExpression Left  { get; }
        public Token Operator    { get; }
        public IExpression Right { get; }
        public DataType    Cast  { get; set; }

        public BinaryExpr(IExpression left, Token op, IExpression right)
        {
            this.Left = left;
            this.Operator = op;
            this.Right = right;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
