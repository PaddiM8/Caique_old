using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    class BinaryExpr : IExpression
    {
        public IExpression Left      { get; }
        public Token       Operator  { get; }
        public IExpression Right     { get; }
        public BaseType    Cast      { get; set; }
        public DataType    DataType  { get; set; }

        public BinaryExpr(IExpression left, Token op, IExpression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public T Accept<T>(IExpressionVisitor<T> expr)
        {
            return expr.Visit(this);
        }
    }
}
