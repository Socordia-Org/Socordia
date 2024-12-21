﻿using Backlang.CodeAnalysis.AST;
using Backlang.Codeanalysis.Parsing;

namespace Backlang.CodeAnalysis.Parsing.ParsePoints.Expressions;

public sealed class SizeOfExpressionParser : IParsePoint
{
    public static AstNode Parse(TokenIterator iterator, Parser parser)
    {
        //sizeof(i32)

        iterator.Match(TokenType.OpenParen);

        var type = TypeLiteral.Parse(iterator, parser);

        iterator.Match(TokenType.CloseParen);

        return SyntaxTree.SizeOf(type);
    }
}