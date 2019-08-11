using System;
using System.Collections.Generic;
using LLVMSharp;

namespace Caique.CodeGen
{
    class ScopeEnv
    {
        public ScopeEnv Parent { get; }
        public bool HasParent { get; }
        private readonly Dictionary<string, LLVMValueRef> Values
            = new Dictionary<string, LLVMValueRef>();

        public ScopeEnv(ScopeEnv parent)
        {
            this.Parent = parent;
            this.HasParent = true;
        }

        public ScopeEnv()
        {
            this.HasParent = false;
        }

        public void Define(string name, LLVMValueRef value)
        {
            Values.Add(name, value);
        }

        public LLVMValueRef Get(string name)
        {
            LLVMValueRef value;
            if (!Values.TryGetValue(name, out value))
            {
                value = Parent.Get(name);
            }

            return value;
        }
    }
}
