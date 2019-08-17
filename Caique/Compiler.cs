using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using LLVMSharp;
using Caique.Models;
using Caique.Scanning;
using Caique.Parsing;
using Caique.Expressions;
using Caique.Statements;
using Caique.CodeGen;

namespace Caique
{
    class Compiler
    {

        private string RootFileLocation { get; }
        public Compiler(string fileLocation)
        {
            this.RootFileLocation = fileLocation;
        }

        public void Compile()
        {
            List<Token> tokens = new Lexer(File.ReadAllText(RootFileLocation)).ScanTokens();
            var parser = new Parser(tokens);
            List<IStatement> statements = parser.Parse();
            new TypeChecker(statements, parser.Functions).CheckTypes();
            var module = new CodeGenerator(statements).GenerateLLVM();

            string moduleError = "";
            LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out moduleError);
            Console.WriteLine(moduleError);

            string error = "";
            LLVM.DumpModule(module);
            LLVM.PrintModuleToFile(module, "test.ll", out error);
            Console.WriteLine(error);
        }
    }
}
