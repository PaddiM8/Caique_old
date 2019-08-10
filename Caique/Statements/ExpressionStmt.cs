using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class ExpressionStmt : IStatement
    {
        public IExpression Expression { get; }

        public ExpressionStmt(IExpression expr)
        {
            this.Expression = expr;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
