using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessageAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessageAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        //public const string DiagnosticId = "MessageAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        //private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        //private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        //private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";
        public const string DiagnosticId = "MessageAnalysis";
        internal const string Title = "Message validation failed";
        internal const string MessageFormat = "Message is not serializable";
        internal const string Description = "Message should be serializable and expose their names.";
        //internal const string Category = "Syntax";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {

            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            
            if (classDeclaration.ImplementsAMessageInterface(context))
            {
                if (!classDeclaration.HasSerializableAttribute(context))
                {
                    var diagnostic = Diagnostic.Create(Rule, classDeclaration.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
                
            }           
        }
    }
    
    public static class Extensions
    {
        public static bool ImplementsAMessageInterface(this ClassDeclarationSyntax classDeclaration, SyntaxNodeAnalysisContext context)
        {
            var bases = classDeclaration.BaseList;
            if (bases?.Types != null)
            {
                foreach (var b in bases.Types)
                {
                    var nodeType = context.SemanticModel.GetTypeInfo(b.Type);
                    if ($"{nodeType.Type.ContainingNamespace.Name}.{nodeType.Type.Name}".Equals("TestConsoleApplication1.ICommand"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasSerializableAttribute(this ClassDeclarationSyntax classDeclaration, SyntaxNodeAnalysisContext context)
        {
            foreach (var attribute in classDeclaration.AttributeLists.SelectMany(al => al.Attributes))
            {
                if (context.SemanticModel.GetTypeInfo(attribute).Type?.ToDisplayString() ==
                    "System.SerializableAttribute")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
