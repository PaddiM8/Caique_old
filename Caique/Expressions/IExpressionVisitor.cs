using System;
using System.Collections.Generic;

namespace Caique.Expressions
{
    interface IExpressionVisitor<T>
    {
        T Visit(BinaryExpr expr);
        T Visit(LiteralExpr expr);
        T Visit(GroupExpr expr);
        T Visit(VariableExpr expr);
        T Visit(CallExpr expr);
    }
}
