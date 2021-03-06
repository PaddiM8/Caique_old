using System;
using System.Collections.Generic;
using LLVMSharp;
using Caique.Logging;
using Caique.Models;

namespace Caique.CodeGen
{
    class Scope
    {
        public Scope Parent    { get; set; }
        public bool  HasParent { get; }

        private readonly Dictionary<string, Tuple<DataType, bool>> Values
            = new Dictionary<string, Tuple<DataType, bool>>();

        public Scope(bool hasParent = false)
        {
            HasParent = hasParent;
        }

        public Scope AddChildScope()
        {
            var newScope = new Scope(true);
            newScope.Parent = this;

            return newScope;
        }

        public void Define(string name, DataType dataType, bool isArgumentVar)
        {
            Values.Add(name, new Tuple<DataType, bool>(dataType, isArgumentVar));
        }

        public Tuple<DataType, bool> Get(string name)
        {
            Tuple<DataType, bool> value;
            if (!Values.TryGetValue(name, out value))
            {
                if (HasParent)
                {
                    value = Parent.Get(name);
                }
                else
                {
                    Reporter.Error(new Pos(0, 0), $"Variable '{name}' is not defined.");
                    value = new Tuple<DataType, bool>(new DataType(BaseType.Unknown, 0), false);
                }
            }

            return value;
        }
    }
}
