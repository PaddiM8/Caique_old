using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class AssignmentStmt : IStatement
    {
        public Token       Identifier { get; }
        public IExpression Value      { get; }

        public AssignmentStmt(Token identifier, IExpression value)
        {
            this.Identifier = identifier;
            this.Value = value;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
