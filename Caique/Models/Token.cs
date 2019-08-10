using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Caique.Models
{
    class Token
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenType    Type        { get; }
        public string       Lexeme      { get; }
        [JsonIgnore]
        public Pos          Position    { get; }
        public object       Literal     { get; set; }
        public DataType     DataType    { get; }

        public Token(TokenType type, string lexeme, Pos? position = null, object literal = null, DataType? dataType = null)
        {
            this.Type = type;
            this.Lexeme = lexeme;
            if (position != null) this.Position = (Pos)position;
            this.Literal = literal;
            if (dataType != null) this.DataType = (DataType)dataType;
        }
    }
}
