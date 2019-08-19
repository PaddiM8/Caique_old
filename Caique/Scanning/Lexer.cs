using System;
using System.Collections.Generic;
using Caique.Models;
using Caique.Logging;

namespace Caique.Scanning
{
    class Lexer
    {
        private string _source;
        private List<Token> _tokens = new List<Token>();
        private int _start = 0;
        private int _current = 0;
        private Pos _position = new Pos(1, 1);
        private static Dictionary<string, TokenType> _keywords =
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

        private static Dictionary<string, DataType> _dataTypes =
            new Dictionary<string, DataType>()
        {
            { "string", DataType.String   },
            { "i1",     DataType.Int1     },
            { "i8",     DataType.Int8     },
            { "i16",    DataType.Int16    },
            { "i32",    DataType.Int32    },
            { "i64",    DataType.Int64    },
            { "i128",   DataType.Int128   },
            { "f16",    DataType.Float16  },
            { "f32",    DataType.Float32  },
            { "f64",    DataType.Float64  },
            { "f80",    DataType.Float80  },
            { "f128",   DataType.Float128 },
            { "bool",   DataType.Boolean  },
        };

        public Lexer(string source)
        {
            this._source = source;
        }

        /// <summary>
        /// Starts the lexing
        /// </summary>
        /// <returns>A list of tokens</returns>
        public List<Token> ScanTokens()
        {
            while (!IsAtEnd())
            {
                _start = _current;
                ScanToken();
            }

            _tokens.Add(new Token(TokenType.EOF, "", _position));
            return _tokens;
        }

        /// <summary>
        /// Advance to the next character and add it(and sometimes characters following that) to the token list as a token.
        /// </summary>
        private void ScanToken()
        {
            char c = Advance();
            switch (c)
            {
                // Skip
                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace.
                    break;
                case '\n':
                    _position.Line++;
                    break;

                // Single character tokens
                case '(': AddToken(TokenType.LeftParen);  break;
                case ')': AddToken(TokenType.RightParen); break;
                case '{': AddToken(TokenType.LeftBrace);  break;
                case '}': AddToken(TokenType.RightBrace); break;
                case '[': AddToken(TokenType.LeftAngle);  break;
                case ']': AddToken(TokenType.RightAngle); break;
                case ',': AddToken(TokenType.Comma);      break;
                case '.': AddToken(TokenType.Dot);        break;
                case '-': AddToken(TokenType.Minus);      break;
                case '+': AddToken(TokenType.Plus);       break;
                case ';': AddToken(TokenType.Semicolon);  break;
                case '*': AddToken(TokenType.Star);       break;
                case '%': AddToken(TokenType.Modulus);    break;

                // Longer tokens
                case '!': AddToken(Match('=') ? TokenType.NotEqual     : TokenType.Bang);    break;
                case '=': AddToken(Match('=') ? TokenType.EqualEqual   : TokenType.Equal);   break;
                case '<': AddToken(Match('=') ? TokenType.LessEqual    : TokenType.Less);    break;
                case '>': AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater); break;
                case '&': if (Match('&')) AddToken(TokenType.And); break;
                case '|': if (Match('|')) AddToken(TokenType.Or); break;
                case '/':
                    if (Match('/'))
                    {
                         while (Peek() != '\n' && !IsAtEnd()) Advance(); // Advance until at end of line (ignoring the comment)
                    }
                    else if (Match('*'))
                    {
                        while (Peek() != '*' && PeekNext() != '/' && !IsAtEnd()) Advance(); // Advance until '*/', ending multi-line comment
                        _current += 2;
                    }
                    else
                    {
                        AddToken(TokenType.Slash);
                    }
                    break;

                // Literals
                case '\"':
                    AddToken(TokenType.String, GetStringLiteral(), DataType.StringConst);
                    break;

                default:
                    if (IsDigit(c))
                    {
                        GetNumberLiteral();
                    }
                    else if (IsAlpha(c))
                    {
                        TokenType type = GetIdentifier();
                        if (type == TokenType.True) AddToken(type, "1", DataType.True);
                        else if (type == TokenType.False) AddToken(type, "0", DataType.True);
                        else AddToken(type);
                    }
                    else
                    {
                        Reporter.Error(_position, "Unexpected character: " + c.ToString());
                    }
                    break;
            }
        }

        /// <summary>
        /// Get string inside double quotes
        /// </summary>
        private string GetStringLiteral()
        {
            // While the next character isn't " and it isn't the last character
            while (Peek() != '"' && !IsAtEnd())
            {
                if (Peek() == '\n') _position.Line++; // Allow multi-line strings
                Advance();
            }

            // Unterminated string.
            if (IsAtEnd())
            {
                Reporter.Error(_position, "Unterminated string.");
                return "";
            }

            // The closing ".
            Advance();

            return _source.Substring(_start + 1, _current - _start - 2); // Grab from the source, excluding the double quotes
        }

        /// <summary>
        /// Get number
        /// </summary>
        private void GetNumberLiteral()
        {
            bool hasDot = false;

            while (IsDigit(Peek())) Advance();

            // If the character is a dot, consume it and continue to look for digits
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                hasDot = true;
                Advance();

                while (IsDigit(Peek())) Advance();
            }

            string numberString = _source.Substring(_start, _current - _start);

            if (Peek() == 'f')
            {
                Advance();
                AddToken(TokenType.Number, numberString, DataType.Float32);
            }
            else if (hasDot)
            {
                AddToken(TokenType.Number, numberString, DataType.Float64);
            }
            else
            {
                AddToken(TokenType.Number, numberString, DataType.Int32); // Should be adjusted when code is generated.
            }
        }

        /// <summary>
        /// Get identifier
        /// </summary>
        /// <returns>Identifier TokenType</returns>
        private TokenType GetIdentifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();

            string text = _source.Substring(_start, _current - _start);
            TokenType type;
            bool isKeyWord = _keywords.TryGetValue(text, out type);
            if (!isKeyWord) type = TokenType.Identifier;

            return type;
        }

        /// <summary>
        /// Move to the next character and return it
        /// </summary>
        private char Advance()
        {
            _current++;
            _position.Column++;
            return _source[_current - 1];
        }

        /// <summary>
        /// Check if the next character is a particular one, if so, move on to the next character and return true.
        /// Otherrwise, simply return false and don't move to the next character.
        /// </summary>
        /// <param name="expected">Expected character</param>
        /// <returns>Whether or not the next character is the expected one</returns>
        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_current] != expected) return false;

            Advance();
            return true;
        }

        /// <summary>
        /// Lookahead, see what the next character is
        /// </summary>
        /// <returns>The next character</returns>
        private char Peek()
        {
            if (IsAtEnd()) return '\0'; // Return null character if at end
            return _source[_current];
        }
        ///
        /// <summary>
        /// Lookahead, see what the 2nd character after the current one is
        /// </summary>
        /// <returns>The next character</returns>
        private char PeekNext()
        {
            if (_current + 1 >= _source.Length) return '\0'; // Return null character if at end
            return _source[_current + 1];
        }

        /// <summary>
        /// Add token without a value
        /// </summary>
        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        /// <summary>
        /// Add a new token to the token list (with value)
        /// </summary>
        private void AddToken(TokenType type, dynamic literal, DataType? dataType = null)
        {
            string text = _source.Substring(_start, _current - _start);
            if (type == TokenType.VariableType)
            {
                dataType = _dataTypes[text];
            }

            if (type == TokenType.True && literal == null) throw new Exception("Börk.");
            _tokens.Add(new Token(type, text, _position, literal, dataType));
        }

        /// <summary>
        /// If the current character is the last one
        /// </summary>
        /// <returns>Whether or not the lexer is at the last character</returns>
        private bool IsAtEnd() => _current >= _source.Length;

        /// <param name="char">Character to check</param>
        /// <returns>Whether or not the character is a digit</returns>
        private bool IsDigit(char c) => c >= '0' && c <= '9';

        /// <param name="char">Character to check</param>
        /// <returns>Whether or not the character is A-z or _</returns>
        private bool IsAlpha(char c) => (c >= 'a' && c <= 'z') ||
                                        (c >= 'A' && c <= 'Z') ||
                                         c == '_';

        /// <param name="c">Character to check</param>
        /// <returns>Whether or not the character is A-z or 0-9 or _</returns>
        private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
    }
}
