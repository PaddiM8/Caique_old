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
        public delegate IExpression ArithmeticFunction();
        // functionName: returnType, parameterTypes...
        public Dictionary<string, DataType[]> Functions =
            new Dictionary<string, DataType[]>()
        {   // This here is very temporary.
            { "printf", new DataType[] { new DataType(BaseType.Void, 0), new DataType(BaseType.String, 0), new DataType(BaseType.Variadic, 0) } },
            { "scanf", new DataType[] { new DataType(BaseType.Void, 0), new DataType(BaseType.String, 0), new DataType(BaseType.Variadic, 0) } },
        };

        private List<Token> _tokens { get; }
        private int _current = 0;

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
            if (Match(TokenType.If))           return If();

            if (Check(TokenType.Identifier))
            {
                // If the current statement has '=', it's an AssignmentStmt
                for (int i = _current; i < _tokens.Count; i++)
                {
                    TokenType type = _tokens[i].Type;
                    if (type == TokenType.Equal) return Assignment();
                    else if (type == TokenType.Semicolon) break;
                }
            }

            return ExpressionStatement();
        }

        public IStatement VarDeclaration()
        {
            Tuple<DataType, List<IExpression>> declarationType = DeclarationType();
            Token identifier = Consume(TokenType.Identifier, "Expected identifier after variable type.");

            if (Match(TokenType.Equal))
            {
                IExpression expr = Expression();
                Consume(TokenType.Semicolon, "Expected ';' after expression.");

                return new VarDeclarationStmt(declarationType.Item1, identifier, declarationType.Item2, expr);
            }

            Consume(TokenType.Semicolon, "Expected ';' after expression.");

            return new VarDeclarationStmt(declarationType.Item1, identifier, declarationType.Item2);
        }

        public IStatement Assignment()
        {
            Token identifier = Consume(TokenType.Identifier, ""); // It is an identifier since it made it through the if statement

            List<IExpression> arrayIndexes = Indexes();

            Consume(TokenType.Equal, "Expected equal sign.");
            IExpression expr = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after expression.");

            return new AssignmentStmt(identifier, expr, arrayIndexes);
        }

        public IStatement Function()
        {
            DataType returnType = Type();
            Token name = Consume(TokenType.Identifier, "Expected function name.");
            Consume(TokenType.LeftParen, "Expected '(' after function name");

            var arguments = new List<Argument>();
            while(!Match(TokenType.RightParen))
            {
                arguments.Add(Argument());

                if (Match(TokenType.RightParen)) break;
                Consume(TokenType.Comma, "Expected ',' after argument name.");
            }

            // Fill DataType array
            var dataTypes = new DataType[arguments.Count + 1];
            dataTypes[0] = returnType;
            for (int i = 0; i < arguments.Count; i++) dataTypes[i+1] = arguments[i].DataType;
            Functions[name.Lexeme] = dataTypes;

            Consume(TokenType.LeftBrace, "Expected block after function declaration.");
            BlockStmt block = (BlockStmt)Block();

            return new FunctionStmt(returnType, name, arguments, block);
        }

        public Argument Argument()
        {
            DataType type = Type();
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

        public IStatement If()
        {
            IExpression condition = Expression();
            IStatement thenBranch = Statement();
            IStatement elseBranch = null;

            if (Match(TokenType.Else))
            {
                elseBranch = Statement();
            }

            return new IfStmt(condition, thenBranch, elseBranch);
        }

        public IStatement ExpressionStatement()
        {
            IExpression expr = Expression();
            Consume(TokenType.Semicolon, "Expected ';' after expression.");

            return new ExpressionStmt(expr);
        }

        public IExpression Expression()
        {
            return LogicalOr();
        }

        public IExpression LogicalOr()
        {
            IExpression expr = LogicalAnd();

            while (Match(TokenType.Or))
            {
                Token op = Previous();
                IExpression right = LogicalAnd();

                return new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        public IExpression LogicalAnd()
        {
            IExpression expr = Equality();

            while (Match(TokenType.And))
            {
                Token op = Previous();
                IExpression right = Equality();

                return new BinaryExpr(expr, op, right);
            }

            return expr;
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
            IExpression expr = Unary();

            while (Match(TokenType.Star, TokenType.Slash))
            {
                Token op = Previous();
                IExpression right = Unary();

                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        public IExpression Unary()
        {
            if (Match(TokenType.Bang, TokenType.Minus))
            {
                return new UnaryExpr(Previous(), Primary());
            }

            return Primary();
        }

        public IExpression Primary()
        {
            if (Match(TokenType.Number, TokenType.True, TokenType.False, TokenType.String)) // Literal
            {
                return new LiteralExpr(Previous(), Indexes());
            }

            if (Match(TokenType.LeftParen)) // Group
            {
                IExpression expr = Expression();
                Consume(TokenType.RightParen, "Expected ')' after expression.");
                return new GroupExpr(expr, Indexes());
            }

            if (Match(TokenType.Identifier))
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
                        Consume(TokenType.Comma, "Expected ')' after expression.");
                    }

                    return new CallExpr(name, parameters, Indexes());
                }

                // Variable expression
                return new VariableExpr(identifier, Indexes());
            }

            throw Error($"Unexpected token '{Peek().Type}'.");
        }

        public List<IExpression> Indexes()
        {
            if (!Match(TokenType.LeftAngle)) return null;

            var arraySizes = new List<IExpression>();
            while (true) // Will break if a RightAngle is matched.
            {
                arraySizes.Add(Expression());
                if (Match(TokenType.RightAngle)) break;
                Consume(TokenType.Comma, "Expected comma.");
            }

            if (arraySizes.Count == 0) Reporter.Error(Previous().Position, "Expected array size specifier.");

            return arraySizes;
        }

        public Tuple<DataType, List<IExpression>> DeclarationType()
        {
            BaseType baseType = Keywords.BaseTypes[Previous().Lexeme];
            DataType dataType = new DataType(baseType, 0);

            // Arrays
            List<IExpression> sizes = Indexes();
            dataType.ArrayDepth = sizes.Count; // Set array depth(how many dimensions, if any) to the amount of specified array sizes.

            return new Tuple<DataType, List<IExpression>>(dataType, sizes);
        }

        public DataType Type()
        {
            string typeString = Consume(TokenType.VariableType, $"Expected type.").Lexeme;
            DataType dataType = new DataType(Keywords.BaseTypes[typeString], 0);

            // Is array
            if (Match(TokenType.LeftAngle))
            {
                while (!Match(TokenType.RightAngle))
                {
                    Consume(TokenType.Comma, "Expected comma.");
                    dataType.ArrayDepth++;
                }

                dataType.ArrayDepth++; // One comma means two dimensions, so +1
            }

            return dataType;
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
                return Previous();
            }
            else
            {
                throw Error(errorMessage);
            }
        }

        /// <summary>
        /// Go to the next token
        /// </summary>
        private Token Advance()
        {
            _current++;

            return _tokens[_current];
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
