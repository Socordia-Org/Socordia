﻿using Socordia.CodeAnalysis.AST;

namespace Socordia.CodeAnalysis.Parsing.ParsePoints.Statements.Loops;

public sealed class DoWhileStatementParser : IParsePoint
{
    public static AstNode Parse(TokenIterator iterator, Parser parser)
    {
        var keywordToken = iterator.Prev;

        var body = Statement.ParseBlock(parser);

        iterator.Match(TokenType.While);

        var cond = Expression.Parse(parser);

        iterator.Match(TokenType.Semicolon);

        return SyntaxTree.DoWhile(body, cond);
    }
}