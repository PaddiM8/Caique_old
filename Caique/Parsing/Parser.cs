using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Caique.Models;
using Caique.Expressions;
using Caique.Statements;
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

        public List<IStatement> Parse()
        {
            var statements = new List<IStatement>();
            while (!IsAtEnd())
            {
                statements.Add(Statement());
            }

            return statements;
            //return Expression();
        }

        public IStatement Statement()
        {
            if (Match(TokenType.VariableType)) return VarDeclaration();
            if (Match(TokenType.Fun))          return Function();
            if (Match(TokenType.LeftBrace))    return Block();
            if (Match(TokenType.Return))       return Return();

            if (Check(TokenType.Identifier) && CheckNext(TokenType.Equal))
            {
                return Assignment();
            }

            return ExpressionStatement();
        }

        public IStatement VarDeclaration()
        {
            DataType dataType = Previous().DataType;
            Token identifier = Consume(TokenType.Identifier, "Expected identifier after variable type keyword.");

            if (Match(TokenType.Equal))
            {
                IExpression expr = Expression();
                Consume(TokenType.Semicolon, "Expected ';' after expression.");

                return new VarDeclarationStmt(dataType, identifier, expr);
            }

            Consume(TokenType.Semicolon, "Expected ';' after expression.");

            return new VarDeclarationStmt(dataType, identifier);
        }

        public IStatement Assignment()
        {
            Token identifier = Previous();
            Consume(TokenType.Equal, "Expected '=' after identifier.");
            IExpression expr = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after expression.");

            return new AssignmentStmt(identifier, expr);
        }

        public IStatement Function()
        {
            DataType returnType = Consume(TokenType.VariableType, "Expected type after 'fn'").DataType;
            Token name = Consume(TokenType.Identifier, "Expected function name.");
            Consume(TokenType.LeftParen, "Expected '(' after function name");

            var arguments = new List<Argument>();
            while(!Match(TokenType.RightParen))
            {
                arguments.Add(Argument());

                if (Match(TokenType.RightParen)) break;
                Consume(TokenType.Comma, "Expected ',' after argument name.");
            }

            Consume(TokenType.LeftBrace, "Expected block after function declaration.");
            BlockStmt block = (BlockStmt)Block();
            return new FunctionStmt(returnType, name, arguments, block);
        }

        public Argument Argument()
        {
            DataType type = Consume(TokenType.VariableType, "Expected type.").DataType;
            Token name = Consume(TokenType.Identifier, "Expected argument name.");

            return new Argument(type, name);
        }

        public IStatement Block()
        {
            var statements = new List<IStatement>();
            while(!Match(TokenType.RightBrace))
            {
                statements.Add(Statement());
            }

            return new BlockStmt(statements);
        }

        public IStatement Return()
        {
            IExpression expr = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after expression.");

            return new ReturnStmt(expr);
        }

        public IStatement ExpressionStatement()
        {
            IExpression expr = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after expression.");

            return new ExpressionStmt(expr);
        }

        public IExpression Expression()
        {
            return Equality();
        }

        public IExpression Equality()
        {
            IExpression expr = Comparison();

            while (Match(TokenType.EqualEqual, TokenType.NotEqual))
            {
                Token op = Previous();
                IExpression right = Comparison();

                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        public IExpression Comparison()
        {
            IExpression expr = Addition();

            while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
            {
                Token op = Previous();
                IExpression right = Addition();

                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        public IExpression Addition()
        {
            IExpression expr = Multiplication();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                Token op = Previous();
                IExpression right = Multiplication();

                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        public IExpression Multiplication()
        {
            IExpression expr = Primary();

            while (Match(TokenType.Star, TokenType.Slash))
            {
                Token op = Previous();
                IExpression right = Primary();

                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        public IExpression Primary()
        {
            if (Match(TokenType.Number, TokenType.True, TokenType.False, TokenType.String)) // Literal
            {
                return new LiteralExpr(Previous());
            }
            else if (Match(TokenType.LeftParen)) // Group
            {
                IExpression expr = Expression();
                Consume(TokenType.RightParen, "Expected ')' after expression.");
                return new GroupExpr(expr);
            }
            else if (Match(TokenType.Identifier))
            {
                Token identifier = Previous();

                if (Match(TokenType.LeftParen)) // Function call
                {
                    Token name = identifier;
                    var parameters = new List<IExpression>();

                    while (!Match(TokenType.RightParen))
                    {
                        parameters.Add(Expression());
                        if (Match(TokenType.RightParen)) break; // Don't expect comma if it's the last parameter
                        Consume(TokenType.Comma, "Expected ',' after expression.");
                    }

                    return new CallExpr(name, parameters);
                }
                else // Variable expression
                {
                    return new VariableExpr(identifier);
                }
            }

            throw Error($"Unexpected token '{Peek().Type}'.");
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
        /// Get the next token.
        /// </summary>
        private Token PeekNext() => _tokens[_current + 1];

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

        // Check if the next token is of the provided type.
        private bool CheckNext(TokenType type)
        {
            TokenType nextType = PeekNext().Type;
            if (IsAtEnd()) return false;
            if (nextType == TokenType.EOF) return false;

            return nextType == type;
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
