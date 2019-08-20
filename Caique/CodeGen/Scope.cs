using System;
using System.Collections.Generic;
using LLVMSharp;
using Caique.Logging;
using Caique.Models;

namespace Caique.CodeGen
{
    class Scope
    {
        public Scope Parent { get; set; }
        public bool HasParent { get; }
        private readonly Dictionary<string, Tuple<BaseType, bool>> Values
            = new Dictionary<string, Tuple<BaseType, bool>>();

        /*public Scope(Scope parent)
        {
            this.Parent = parent;
            this.HasParent = true;
        }*/

        public Scope(bool hasParent = false)
        {
            this.HasParent = hasParent;
        }

        public Scope AddChildScope()
        {
            var newScope = new Scope(true);
            newScope.Parent = this;
            return newScope;
        }

        public void Define(string name, BaseType baseType, bool isArgumentVar)
        {
            Values.Add(name, new Tuple<BaseType, bool>(baseType, isArgumentVar));
        }

        public Tuple<BaseType, bool> Get(string name)
        {
            Tuple<BaseType, bool> value;
            if (!Values.TryGetValue(name, out value))
            {
                if (HasParent)
                {
                    value = Parent.Get(name);
                }
                else
                {
                    Reporter.Error(new Pos(0, 0), $"Variable '{name}' is not defined.");
                    value = new Tuple<BaseType, bool>(BaseType.Unknown, false);
                }
            }

            return value;
        }
    }
}
