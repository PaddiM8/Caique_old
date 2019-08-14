using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique.Expressions
{
    interface IExpression
    {
        DataType Cast { get; set; }
        T Accept<T>(IExpressionVisitor<T> expr);
    }
}
