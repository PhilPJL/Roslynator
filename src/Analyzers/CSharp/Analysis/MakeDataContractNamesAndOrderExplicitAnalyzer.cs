using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Analysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Roslynator.CSharp.CSharp.Analysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeDataContractNamesAndOrderExplicitAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.StructDeclaration);
        }

        public static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;

            INamedTypeSymbol symbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken);

            var dataContractAttribute = symbol.GetAttribute(MetadataNames.System_Runtime_Serialization_DataContractAttribute);

            if (dataContractAttribute == null)
            {
                return;
            }

            const string Name = "Name";
            const string Order = "Order";

            if (!dataContractAttribute.NamedArguments.Any(kvp => kvp.Key == Name))
            {
                DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
            }
            else
            {
                var members = symbol.GetMembers()
                    .Select(m => new { Member = m, DataMemberAttribute = m.GetAttribute(MetadataNames.System_Runtime_Serialization_DataMemberAttribute) })
                    .Where(m => m.DataMemberAttribute != null)
                    .ToList();

                if (members.Any(m => !m.DataMemberAttribute.NamedArguments.Any(kvp => kvp.Key == Name)))
                {
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
                }
                else if (members.Any(m => !m.DataMemberAttribute.NamedArguments.Any(kvp => kvp.Key == Order)))
                {
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
                }
            }
        }
    }
}
