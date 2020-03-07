using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using RequestAnalyzer;

namespace RequestAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new RequestAnalyzer();
        }

        [TestMethod]
        public void Should_Return_Diagnostic_For_Missing_Command_Handler()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace Foo
    {
        public interface IRequest<TResult>
        {
        }

        public sealed class MyRequest : IRequest<int>
        {
        }

        public sealed class MyOtherRequest : IRequest<int>
        {
        }

        public sealed class MyRequestHandler : RequestHandler<MyRequest, int>
        {
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "RequestAnalyzer",
                Message = "The request 'MyOtherRequest' does not have an associated handler.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 19, 29)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
    }
}
