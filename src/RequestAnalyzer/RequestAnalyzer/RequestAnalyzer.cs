using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RequestAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RequestAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RequestAnalyzer";
        private static DiagnosticDescriptor MissingRequestHandler = new DiagnosticDescriptor(
            DiagnosticId, "Request does not have an associated handler",
            "The request '{0}' does not have an associated handler.", "Maintenance",
            DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: "Requests should have an associated handler");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(MissingRequestHandler); }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            analysisContext.EnableConcurrentExecution();
            analysisContext.RegisterCompilationStartAction(Analyze);
        }

        private void Analyze(CompilationStartAnalysisContext startContext)
        {
            var handlers = new ConcurrentBag<INamedTypeSymbol>();
            var requests = new ConcurrentBag<INamedTypeSymbol>();

            startContext.RegisterSymbolAction(context =>
            {
                var type = (INamedTypeSymbol)context.Symbol;
                if (type.TypeKind == TypeKind.Class)
                {
                    if (type.IsAbstract || type.IsStatic)
                    {
                        return;
                    }

                    // Is this a request handler?
                    if (type.BaseType != null && type.BaseType.Name == "RequestHandler" &&
                        type.BaseType.TypeArguments.Length == 2)
                    {
                        handlers.Add(type);
                    }
                    // Is this a request?
                    else if (type.Interfaces.Any(x => x.Name == "IRequest" && x.TypeArguments.Length == 1))
                    {
                        requests.Add(type);
                    }
                }
            }, SymbolKind.NamedType);

            startContext.RegisterCompilationEndAction(context =>
            {
                // Check all requests.
                foreach (var request in requests)
                {
                    var found = false;

                    // Now check all handlers.
                    foreach (var handler in handlers)
                    {
                        // Is this handler an implementation for our request?
                        // I.e. RequestHandler<Foo, int> for Foo? Check this by comparing
                        // the first type argument with the name of the request.
                        if (handler.BaseType.TypeArguments[0].Name == request.Name)
                        {
                            // Yes, we found it.
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Report a diagnostic for the first occurance of the symbol.
                        context.ReportDiagnostic(Diagnostic.Create(
                            MissingRequestHandler,
                            request.Locations.FirstOrDefault(),
                            request.Name));
                    }
                }
            });
        }
    }
}
