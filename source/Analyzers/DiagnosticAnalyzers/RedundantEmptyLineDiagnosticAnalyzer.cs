﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.DiagnosticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RedundantEmptyLineDiagnosticAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.RemoveRedundantEmptyLine); }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.RegisterSyntaxNodeAction(f => AnalyzeClassDeclaration(f), SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(f => AnalyzeStructDeclaration(f), SyntaxKind.StructDeclaration);
            context.RegisterSyntaxNodeAction(f => AnalyzeInterfaceDeclaration(f), SyntaxKind.InterfaceDeclaration);
            context.RegisterSyntaxNodeAction(f => AnalyzeNamespaceDeclaration(f), SyntaxKind.NamespaceDeclaration);
        }

        public void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var declaration = (ClassDeclarationSyntax)context.Node;

            AnalyzeDeclaration(
                context,
                declaration.Members,
                declaration.OpenBraceToken,
                declaration.CloseBraceToken);
        }

        private void AnalyzeStructDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var declaration = (StructDeclarationSyntax)context.Node;

            AnalyzeDeclaration(
                context,
                declaration.Members,
                declaration.OpenBraceToken,
                declaration.CloseBraceToken);
        }

        private void AnalyzeInterfaceDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var declaration = (InterfaceDeclarationSyntax)context.Node;

            AnalyzeDeclaration(
                context,
                declaration.Members,
                declaration.OpenBraceToken,
                declaration.CloseBraceToken);
        }

        private void AnalyzeNamespaceDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var declaration = (NamespaceDeclarationSyntax)context.Node;

            AnalyzeNamespaceDeclaration(context, declaration);
        }

        private static void AnalyzeDeclaration(
            SyntaxNodeAnalysisContext context,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken openBrace,
            SyntaxToken closeBrace)
        {
            if (members.Count > 0)
            {
                AnalyzeStart(context, members[0], openBrace);
                AnalyzeEnd(context, members[members.Count - 1], closeBrace);
            }
        }

        private static void AnalyzeNamespaceDeclaration(
            SyntaxNodeAnalysisContext context,
            NamespaceDeclarationSyntax declaration)
        {
            if (declaration.Externs.Count > 0)
            {
                AnalyzeStart(context, declaration.Externs[0], declaration.OpenBraceToken);
            }
            else if (declaration.Usings.Count > 0)
            {
                AnalyzeStart(context, declaration.Usings[0], declaration.OpenBraceToken);
            }
            else if (declaration.Members.Count > 0)
            {
                AnalyzeStart(context, declaration.Members[0], declaration.OpenBraceToken);
            }

            if (declaration.Members.Count > 0)
                AnalyzeEnd(context, declaration.Members.Last(), declaration.CloseBraceToken);
        }

        public static void AnalyzeBlock(SyntaxNodeAnalysisContext context, BlockSyntax block)
        {
            if (block.Statements.Count > 0)
            {
                AnalyzeStart(context, block.Statements[0], block.OpenBraceToken);
                AnalyzeEnd(context, block.Statements[block.Statements.Count - 1], block.CloseBraceToken);
            }
        }

        private static void AnalyzeStart(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            SyntaxToken brace)
        {
            if (!brace.IsMissing
                && (node.GetSpanStartLine() - brace.GetSpanEndLine()) > 1)
            {
                TextSpan? span = GetEmptyLineSpan(node.GetLeadingTrivia(), isEnd: false);

                if (span != null)
                {
                    context.ReportDiagnostic(
                        DiagnosticDescriptors.RemoveRedundantEmptyLine,
                        Location.Create(context.Node.SyntaxTree, span.Value));
                }
            }
        }

        private static void AnalyzeEnd(
            SyntaxNodeAnalysisContext context,
            SyntaxNode node,
            SyntaxToken brace)
        {
            if (!brace.IsMissing)
            {
                int braceLine = brace.GetSpanStartLine();

                if (braceLine - node.GetSpanEndLine() > 1)
                {
                    TextSpan? span = GetEmptyLineSpan(brace.LeadingTrivia, isEnd: true);

                    if (span != null
                        && !IsEmptyLastLineInDoStatement(node, braceLine, span.Value))
                    {
                        context.ReportDiagnostic(
                            DiagnosticDescriptors.RemoveRedundantEmptyLine,
                            Location.Create(context.Node.SyntaxTree, span.Value));
                    }
                }
            }
        }

        private static bool IsEmptyLastLineInDoStatement(
            SyntaxNode node,
            int closeBraceLine,
            TextSpan span)
        {
            SyntaxNode parent = node.Parent;

            if (parent?.IsKind(SyntaxKind.Block) == true)
            {
                parent = parent.Parent;

                if (parent?.IsKind(SyntaxKind.DoStatement) == true)
                {
                    var doStatement = (DoStatementSyntax)parent;

                    int emptyLine = doStatement.SyntaxTree.GetLineSpan(span).EndLine();

                    if (emptyLine == closeBraceLine)
                    {
                        int whileKeywordLine = doStatement.WhileKeyword.GetSpanStartLine();

                        if (closeBraceLine == whileKeywordLine)
                            return true;
                    }
                }
            }

            return false;
        }

        private static TextSpan? GetEmptyLineSpan(
            SyntaxTriviaList triviaList,
            bool isEnd)
        {
            int i = 0;

            foreach (SyntaxTrivia trivia in triviaList)
            {
                if (trivia.IsEndOfLineTrivia())
                {
                    if (isEnd)
                    {
                        for (int j = i + 1; j < triviaList.Count; j++)
                        {
                            if (!triviaList[j].IsWhitespaceOrEndOfLineTrivia())
                                return null;
                        }
                    }

                    return TextSpan.FromBounds(triviaList.Span.Start, trivia.Span.End);
                }
                else if (!trivia.IsWhitespaceTrivia())
                {
                    return null;
                }

                i++;
            }

            return null;
        }
    }
}
