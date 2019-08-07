using System;
using System.Collections.Generic;

namespace Caique.Models
{
    class Token
    {
        public TokenType  Type        { get; }
        public string     Lexeme      { get; }
        public Pos        Position    { get; }
        public object     Literal     { get; set; }

        public Token(TokenType type, string lexeme, Pos? position = null, object literal = null)
        {
            this.Type = type;
            this.Lexeme = lexeme;
            if (position != null) this.Position = (Pos)position;
            this.Literal = literal;
        }
    }
}
