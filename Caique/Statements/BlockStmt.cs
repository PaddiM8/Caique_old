using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class BlockStmt : IStatement
    {
        public List<IStatement> Statements { get; }

        public BlockStmt(List<IStatement> statements)
        {
            this.Statements = statements;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
