﻿using Backlang.CodeAnalysis.AST;
using Backlang.Codeanalysis.Parsing;

namespace Backlang.CodeAnalysis.Parsing.ParsePoints.Statements;

public sealed class ExpressionStatementParser : IParsePoint
{
    public static AstNode Parse(TokenIterator iterator, Parser parser)
    {
        var expr = Expression.Parse(parser);

        iterator.Match(TokenType.Semicolon);

        return expr.WithRange(expr.Range.StartIndex, iterator.Prev.End);
    }
}