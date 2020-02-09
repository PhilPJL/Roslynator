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

            if (dataContractAttribute.NamedArguments.All(kvp => kvp.Key != Name) || dataContractAttribute.NamedArguments.Any(kvp => kvp.Key == Name && string.IsNullOrWhiteSpace(kvp.Value.Value?.ToString())))
            {
                DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
            }
            else
            {
                var members = symbol.GetMembers()
                    .Select(m => new { Member = m, DataMemberAttribute = m.GetAttribute(MetadataNames.System_Runtime_Serialization_DataMemberAttribute) })
                    .Where(m => m.DataMemberAttribute != null)
                    .ToList();

                // No name arg
                if (members.Any(m => !m.DataMemberAttribute.NamedArguments.Any(kvp => kvp.Key == Name)))
                {
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
                }
                // No order
                else if (members.Any(m => !m.DataMemberAttribute.NamedArguments.Any(kvp => kvp.Key == Order)))
                {
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
                }
                // Name arg present, but value missing or empty
                else if (members.Any(m => m.DataMemberAttribute.NamedArguments.Any(kvp => kvp.Key == Name && string.IsNullOrWhiteSpace(kvp.Value.Value?.ToString()))))
                {
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
                }
                // Order arg present, but value missing
                else if (members.Any(m => m.DataMemberAttribute.NamedArguments.Any(kvp => kvp.Key == Order && ((int?)kvp.Value.Value) == null)))
                {
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
                }
                // Duplicate order
                else if(members
                    .Select(m => (int?)m.DataMemberAttribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == Order).Value.Value)
                    .Where(o => o.HasValue)
                    .Select(o => o.Value)
                    .GroupBy(o => o)
                    .Any(g => g.Count() > 1))
                {
                    DiagnosticHelpers.ReportDiagnostic(context, DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit, typeDeclaration.Identifier);
                }

                // Duplicate name is an error and should be a separate analyzer
            }
        }
    }
}
