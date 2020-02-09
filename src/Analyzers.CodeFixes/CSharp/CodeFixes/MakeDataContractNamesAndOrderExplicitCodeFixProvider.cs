using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CodeFixes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeDataContractNamesAndOrderExplicitCodeFixProvider))]
    [Shared]
    public class MakeDataContractNamesAndOrderExplicitCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out TypeDeclarationSyntax typeDeclaration))
                return;

            Diagnostic diagnostic = context.Diagnostics[0];

            var codeAction = CodeAction.Create(
                "Make DataContract name and DataMember name & order explicit.",
                cancellationToken =>
                {
                    return MakeDataContractAndMembersExplicit(
                        context.Document,
                        typeDeclaration,
                        cancellationToken);
                },
                GetEquivalenceKey(diagnostic));

            context.RegisterCodeFix(codeAction, diagnostic);
        }

        private static async Task<Document> MakeDataContractAndMembersExplicit(
            Document document, TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeDeclarationSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            var dataContractAttribute = typeDeclarationSymbol.GetAttribute(MetadataNames.System_Runtime_Serialization_DataContractAttribute);

            const string Name = "Name";
            const string Order = "Order";

            var replacementNodes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();

            // Add name argument to DataContract if missing and sort argument list
            var dataContractAttributeSyntax = (AttributeSyntax)dataContractAttribute.ApplicationSyntaxReference.GetSyntax(cancellationToken);
            var dcArgList = dataContractAttributeSyntax.ArgumentList;

            var dcNameArgument = dcArgList?.Arguments.SingleOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == Name);
            var dcNameExpression = dcNameArgument?.Expression as LiteralExpressionSyntax;

            if (dcArgList == null || dcNameArgument == null || string.IsNullOrWhiteSpace(dcNameExpression?.Token.ValueText))
            {
                var newNameArgument = AttributeArgument(NameEquals(IdentifierName(Name)), StringLiteralExpression(typeDeclaration.Identifier.ValueText));

                var arguments = (dcArgList == null) ? new[] { newNameArgument } :
                        (IEnumerable<AttributeArgumentSyntax>)(dcArgList.Arguments.Where(a => a != dcNameArgument)).Concat(new[] { newNameArgument }).OrderBy(a => a.NameEquals.Name.Identifier.ValueText);

                var newDataContractAttribute = Attribute(IdentifierName(dataContractAttributeSyntax.Name.ToString()), AttributeArgumentList(arguments.ToArray()));

                replacementNodes.Add((dataContractAttributeSyntax, newDataContractAttribute));
            }

            // Add name and order to DataMembers
            var members = typeDeclarationSymbol.GetMembers()
                .Where(m => m.IsKind(SymbolKind.Field) || m.IsKind(SymbolKind.Property))
                .Where(m => m.HasAttribute(MetadataNames.System_Runtime_Serialization_DataMemberAttribute))
                .Select(m =>
                {
                    var dataMemberAttributeSyntax = (AttributeSyntax)(m.GetAttribute(MetadataNames.System_Runtime_Serialization_DataMemberAttribute).ApplicationSyntaxReference.GetSyntax(cancellationToken));
                    var dmArgList = dataMemberAttributeSyntax.ArgumentList;

                    var dmNameExpression = dmArgList?.Arguments.SingleOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == Name)?.Expression as LiteralExpressionSyntax;
                    var dmOrderExpression = dmArgList?.Arguments.SingleOrDefault(a => a.NameEquals?.Name.Identifier.ValueText == Order)?.Expression as LiteralExpressionSyntax;

                    return new
                    {
                        dataMemberAttributeSyntax,

                        Name = (string.IsNullOrWhiteSpace(dmNameExpression?.Token.ValueText)) ? m.Name : dmNameExpression?.Token.ValueText,
                        Order = (int?)dmOrderExpression?.Token.Value
                    };
                })
                // Members without an explit Order come first
                .OrderByDescending(m => m.Order == null)
                // Then by explicit order
                .ThenBy(m => m.Order)
                // Then by name (ordinal)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .Select((m, i) => new
                {
                    DataMemberAttributeSyntax = m.dataMemberAttributeSyntax,
                    m.Name,
                    // Apply an explicit order
                    Order = i + 1
                })
                .ToList();

            foreach (var m in members)
            {
                var dataMemberAttributeSyntax = m.DataMemberAttributeSyntax;

                var newNameArgument = AttributeArgument(NameEquals(IdentifierName(Name)), StringLiteralExpression(m.Name));
                var newOrderArgument = AttributeArgument(NameEquals(IdentifierName(Order)), NumericLiteralExpression(m.Order));

                IEnumerable<AttributeArgumentSyntax> newArgs = new List<AttributeArgumentSyntax> { newNameArgument, newOrderArgument };

                if (dataMemberAttributeSyntax.ArgumentList != null)
                {
                    newArgs = newArgs
                        .Concat(dataMemberAttributeSyntax.ArgumentList.Arguments
                        .Where(a => (a.NameEquals?.Name.Identifier.ValueText != Name) && (a.NameEquals?.Name.Identifier.ValueText != Order)));
                }

                var arguments = newArgs.OrderBy(a => a.NameEquals.Name.Identifier.ValueText);

                var newDataMemberAttribute = Attribute(IdentifierName(dataMemberAttributeSyntax.Name.ToString()), AttributeArgumentList(arguments.ToArray()));

                replacementNodes.Add((dataMemberAttributeSyntax, newDataMemberAttribute));
            }

            return await document.ReplaceNodesAsync(replacementNodes.Select(r => r.oldNode), GetReplacement, cancellationToken).ConfigureAwait(false);

            SyntaxNode GetReplacement(SyntaxNode n1, SyntaxNode _) => replacementNodes.Single(r => r.oldNode == n1).newNode;
        }
    }
}
