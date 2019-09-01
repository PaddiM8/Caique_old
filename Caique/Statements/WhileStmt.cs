using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class WhileStmt : IStatement
    {
        public IExpression Condition { get; }
        public IStatement  Branch    { get; }

        public WhileStmt(IExpression condition, IStatement branch)
        {
            Condition  = condition;
            Branch = branch;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
