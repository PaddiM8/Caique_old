using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class VarDeclarationStmt : IStatement
    {
        public BaseType          BaseType   { get; }
        public Token             Identifier { get; }
        public List<IExpression> ArraySizes { get; }
        public IExpression       Value      { get; }

        public VarDeclarationStmt(BaseType baseType, Token identifier, List<IExpression> arraySizes, IExpression value = null)
        {
            this.BaseType = baseType;
            this.Identifier = identifier;
            this.ArraySizes = arraySizes;
            if (value != null) this.Value = value;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
