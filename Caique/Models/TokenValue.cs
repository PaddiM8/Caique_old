using System;
using System.Collections.Generic;
using System.Collections;

namespace Caique.Models
{
    struct TokenValue<T>
    {
        public T Value;

        public TokenValue(T value)
        {
            this.Value = value;
        }
    }
}
