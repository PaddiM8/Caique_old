using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class IfStmt : IStatement
    {
        public IExpression Condition  { get; }
        public IStatement  ThenBranch { get; }
        public IStatement  ElseBranch { get; }

        public IfStmt(IExpression condition, IStatement thenBranch, IStatement elseBranch = null)
        {
            this.Condition  = condition;
            this.ThenBranch = thenBranch;
            this.ElseBranch = elseBranch;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
