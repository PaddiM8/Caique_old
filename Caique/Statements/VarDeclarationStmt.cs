using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class VarDeclarationStmt : IStatement
    {
        public DataType    DataType   { get; }
        public Token       Identifier { get; }
        public IExpression Value      { get; }

        public VarDeclarationStmt(DataType dataType, Token identifier, IExpression value = null)
        {
            this.DataType = dataType;
            this.Identifier = identifier;
            if (value != null) this.Value = value;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
