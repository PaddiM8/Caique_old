using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Expressions;

namespace Caique.Statements
{
    class ForStmt : IStatement
    {
        public Token VarName         { get; }
        public IExpression StartVal  { get; }
        public IExpression MaxVal    { get; }
        public IExpression Increment { get; }
        public IStatement  Branch    { get; }

        public ForStmt(Token varName, IExpression startVal, IExpression maxVal,
                       IExpression increment, IStatement branch)
        {
            VarName = varName;
            StartVal = startVal;
            MaxVal = maxVal;
            Increment = increment;
            Branch = branch;
        }

        public ForStmt(Token varName, IExpression startVal, IExpression maxVal, IStatement branch)
        {
            VarName = varName;
            StartVal = startVal;
            MaxVal = maxVal;
            Branch = branch;
        }

        public T Accept<T>(IStatementVisitor<T> stmt)
        {
            return stmt.Visit(this);
        }
    }
}
