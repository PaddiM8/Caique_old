using System;
using System.Collections.Generic;

namespace Caique.Expressions
{
    interface IExpression
    {
        T Accept<T>(IExpressionVisitor<T> expr);
    }
}
