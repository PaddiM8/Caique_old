using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class ReturnStmt : IStatement
    {
        public IExpression Expression { get; }

        public ReturnStmt(IExpression expr)
        {
            Expression = expr;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
