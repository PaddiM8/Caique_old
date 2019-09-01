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
using Caique.Logging;

namespace Caique
{
    class Compiler
    {
        private string _rootFileLocation { get; }

        public Compiler(string fileLocation)
        {
            _rootFileLocation = fileLocation;
        }

        public void Compile()
        {
            List<Token> tokens = new Lexer(File.ReadAllText(_rootFileLocation)).ScanTokens();
            //Console.WriteLine(JsonConvert.SerializeObject(tokens));
            var parser = new Parser(tokens);
            List<IStatement> statements = parser.Parse();
            new TypeChecker(statements, parser.Functions).CheckTypes();

            // Don't continue if previous steps generated errors.
            if (Reporter.ErrorList.Count > 0) return;

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
