using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class FunctionStmt : IStatement
    {
        public DataType           ReturnType { get; }
        public Token              Name       { get; }
        public List<Argument>     Arguments  { get; }
        public BlockStmt          Block      { get; }

        public FunctionStmt(DataType returnType, Token name, List<Argument> arguments, BlockStmt block)
        {
            ReturnType = returnType;
            Name = name;
            Arguments = arguments;
            Block = block;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
