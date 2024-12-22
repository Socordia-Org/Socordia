﻿using Socordia.CodeAnalysis.AST;

namespace Socordia.CodeAnalysis.Parsing.ParsePoints.Statements;

public class ThrowStatementParser : IParsePoint
{
    public static AstNode Parse(TokenIterator iterator, Parser parser)
    {
        var keywordToken = iterator.Prev;

        if (!iterator.IsMatch(TokenType.Semicolon))
        {
            var arg = Expression.Parse(parser);
            iterator.Match(TokenType.Semicolon);

            return SyntaxTree.Throw(arg);
        }

        return null;
    }
}