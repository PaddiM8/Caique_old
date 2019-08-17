using System;
using System.Collections.Generic;

namespace Caique
{
    public enum TokenType
    {
        // Single-character tokens
        LeftParen, RightParen, LeftBrace, RightBrace,
        Comma, Dot, Minus, Plus, Semicolon, Slash, Star,
        Modulus,

        // One or two character tokens
        Bang, NotEqual,
        Equal, EqualEqual,
        Greater, GreaterEqual,
        Less, LessEqual,

        // Literals
        Identifier, String, Number,

        VariableType,

        // Keywords
        And, Class, Else, False, Fun, For, If, Null, Or,
        Print, Return, Super, This, True, While, Count,
        Break, Continue,

        EOF
    }

    static class TokenTypeMethods
    {
        public static bool IsComparisonOperator(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.EqualEqual:
                case TokenType.NotEqual:
                case TokenType.Greater:
                case TokenType.GreaterEqual:
                case TokenType.Less:
                case TokenType.LessEqual:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsArithmeticOperator(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Minus:
                case TokenType.Slash:
                case TokenType.Star:
                case TokenType.Modulus:
                case TokenType.Plus:
                    return true;
                default:
                    return false;
            }
        }
    }
}
