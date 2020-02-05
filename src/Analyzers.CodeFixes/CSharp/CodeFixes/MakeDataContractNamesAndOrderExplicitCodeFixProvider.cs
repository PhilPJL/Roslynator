using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CodeFixes;
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
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out TypeDeclarationSyntax typeDeclaration))
                return;

            Diagnostic diagnostic = context.Diagnostics[0];

            var codeAction = CodeAction.Create(
                "Make DataContract/DataMember name/order explicit",
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
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeDeclarationSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            var dataContractAttribute = typeDeclarationSymbol.GetAttribute(MetadataNames.System_Runtime_Serialization_DataContractAttribute);

            const string Name = "Name";

            // Add name argument to DataContract if missing and sort argument list
            if (dataContractAttribute.NamedArguments.All(kvp => kvp.Key != Name))
            {
                var dataContractAttributeSyntax = (AttributeSyntax)dataContractAttribute.ApplicationSyntaxReference.GetSyntax(cancellationToken);
                var nameArgument = AttributeArgument(NameEquals(IdentifierName(Name)), StringLiteralExpression(typeDeclaration.Identifier.ValueText));

                // Get existing arguments, add 'Name' and sort
                var arguments = dataContractAttributeSyntax.ArgumentList.Arguments.Add(nameArgument).OrderBy(a => a.NameEquals.Name.Identifier.ValueText);

                var newDataContractAttribute = Attribute(IdentifierName(dataContractAttributeSyntax.Name.ToString()), AttributeArgumentList(arguments.ToArray()));

                return await document.ReplaceNodeAsync(dataContractAttributeSyntax, newDataContractAttribute, cancellationToken);
            }

            // TODO: add names to DataMembers
            // TODO: add order to DataMembers

            return document;
        }
    }
}
