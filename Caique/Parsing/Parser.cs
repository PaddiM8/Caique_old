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
            return Addition();
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
            if (Match(TokenType.Number))
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

        private Token Peek() => _tokens[_current];

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

        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;

            return Peek().Type == type;
        }

        private ParsingException Error(string errorMessage)
        {
            Reporter.Error(Peek().Position, errorMessage); // Show error separately, since the exception will be caught
            throw new ParsingException();
        }

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

        private void Advance()
        {
            _current++;
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }
    }
}
