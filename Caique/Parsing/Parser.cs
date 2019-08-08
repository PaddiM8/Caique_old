using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Caique.Models;
using Caique.Expressions;
using Caique.Logging;

namespace Caique.Parsing
{
    class Parser
    {
        private List<Token> _tokens { get; }
        private int _current = 0;
        public delegate IExpression ArithmeticFunction();

        public Parser(List<Token> tokens)
        {
            this._tokens = tokens;
        }

        public IExpression Parse()
        {
            return Expression();
        }

        public IExpression Expression()
        {
            return Equality();
        }

        public IExpression Equality()
        {
            return GetBinaryExpression(Comparison, TokenType.EqualEqual, TokenType.NotEqual);
        }

        public IExpression Comparison()
        {
            return GetBinaryExpression(Addition,
                                       TokenType.Greater,
                                       TokenType.GreaterEqual,
                                       TokenType.Less,
                                       TokenType.LessEqual);
        }

        public IExpression Addition()
        {
            return GetBinaryExpression(Multiplication, TokenType.Plus, TokenType.Minus);
        }

        public IExpression Multiplication()
        {
            return GetBinaryExpression(Primary, TokenType.Star, TokenType.Slash);
        }

        public IExpression Primary()
        {
            if (Match(TokenType.Number, TokenType.True, TokenType.False))
            {
                return new LiteralExpr(Previous());
            }
            else if (Match(TokenType.LeftParen))
            {
                IExpression expr = Expression();
                Consume(TokenType.RightParen, "Expected ')' after expression.");
                return new GroupExpr(expr);
            }

            throw Error("Unexpected token.");
        }

        /// <summary>
        /// Parse binary expression.
        /// </summary>
        private IExpression GetBinaryExpression(ArithmeticFunction func, params TokenType[] tokenTypes)
        {
            IExpression expr = func();

            while (Match(tokenTypes))
            {
                Token op = Previous();
                IExpression right = func();

                return new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        /// <summary>
        /// Get the current token.
        /// </summary>
        private Token Peek() => _tokens[_current];

        /// <summary>
        /// If the current token is of any of the provided types, advance and return true. Otherwise return false.
        /// </summary>
        private bool Match(params TokenType[] tokenTypes)
        {
            foreach (var tokenType in tokenTypes)
            {
                if (Check(tokenType))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return the previous token.
        /// </summary>
        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        /// <summary>
        /// Check if the current token is of the provided type.
        /// </summary>
        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;

            return Peek().Type == type;
        }

        /// <summary>
        /// Log error and throw parsing exception. The exception will be caught, after that the parser will synchronize.
        /// </summary>
        private ParsingException Error(string errorMessage)
        {
            Reporter.Error(Peek().Position, errorMessage); // Show error separately, since the exception will be caught
            throw new ParsingException();
        }

        /// <summary>
        /// Check if the current token is the same type as the provided type, if so, advance. Otherwise, log error
        /// </summary>
        private Token Consume(TokenType type, string errorMessage)
        {
            if (IsAtEnd()) throw Error(errorMessage);

            if (Peek().Type == type)
            {
                Advance();
                return Previous(); // Since it advanced
            }
            else
            {
                throw Error(errorMessage);
            }
        }

        /// <summary>
        /// Go to the next token
        /// </summary>
        private void Advance()
        {
            _current++;
        }

        /// <summary>
        /// If the current token is the last one
        /// </summary>
        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }
    }
}
