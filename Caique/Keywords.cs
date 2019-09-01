using System;
using System.Collections.Generic;
using Caique.Models;

namespace Caique
{
    static class Keywords
    {
        public static Dictionary<string, TokenType> TokenTypes =
            new Dictionary<string, TokenType>()
            {
                { "class",     TokenType.Class        },
                { "else",      TokenType.Else         },
                { "false",     TokenType.False        },
                { "for",       TokenType.For          },
                { "fn",        TokenType.Fun          },
                { "break",     TokenType.Break        },
                { "continue",  TokenType.Continue     },
                { "if",        TokenType.If           },
                { "null",      TokenType.Null         },
                { "print",     TokenType.Print        },
                { "return",    TokenType.Return       },
                { "super",     TokenType.Super        },
                { "this",      TokenType.This         },
                { "true",      TokenType.True         },
                { "string",    TokenType.VariableType },
                { "i1",        TokenType.VariableType },
                { "i8",        TokenType.VariableType },
                { "i16",       TokenType.VariableType },
                { "i32",       TokenType.VariableType },
                { "i64",       TokenType.VariableType },
                { "i128",      TokenType.VariableType },
                { "f16",       TokenType.VariableType },
                { "f32",       TokenType.VariableType },
                { "f64",       TokenType.VariableType },
                { "f80",       TokenType.VariableType },
                { "f128",      TokenType.VariableType },
                { "bool",      TokenType.VariableType },
                { "while",     TokenType.While        },
                { "count",     TokenType.Count        },
            };

        public static Dictionary<string, BaseType> BaseTypes =
            new Dictionary<string, BaseType>()
            {
                { "string", BaseType.String   },
                { "i1",     BaseType.Int1     },
                { "i8",     BaseType.Int8     },
                { "i16",    BaseType.Int16    },
                { "i32",    BaseType.Int32    },
                { "i64",    BaseType.Int64    },
                { "i128",   BaseType.Int128   },
                { "f16",    BaseType.Float16  },
                { "f32",    BaseType.Float32  },
                { "f64",    BaseType.Float64  },
                { "f80",    BaseType.Float80  },
                { "f128",   BaseType.Float128 },
                { "bool",   BaseType.Boolean  },
            };
    }
}
