using System;
using System.Collections.Generic;

namespace Caique.Statements
{
    interface IStatementVisitor<T>
    {
        T Visit(VarDeclarationStmt stmt);
        T Visit(ExpressionStmt stmt);
        T Visit(AssignmentStmt stmt);
        T Visit(FunctionStmt stmt);
        T Visit(BlockStmt stmt);
        T Visit(ReturnStmt stmt);
        T Visit(IfStmt stmt);
        T Visit(ForStmt stmt);
        T Visit(WhileStmt stmt);
    }
}
