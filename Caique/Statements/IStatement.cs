using System;
using System.Collections.Generic;

namespace Caique.Statements
{
    interface IStatement
    {
        T Accept<T>(IStatementVisitor<T> stmt);
    }
}
