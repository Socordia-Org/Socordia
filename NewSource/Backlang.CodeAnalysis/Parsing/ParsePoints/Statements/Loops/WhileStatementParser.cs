﻿using Backlang.CodeAnalysis.AST;
using Backlang.Codeanalysis.Parsing;

namespace Backlang.CodeAnalysis.Parsing.ParsePoints.Statements.Loops;

public sealed class WhileStatementParser : IParsePoint
{
    public static AstNode Parse(TokenIterator iterator, Parser parser)
    {
        var keywordToken = iterator.Prev;

        var cond = Expression.Parse(parser);
        var body = Statement.ParseOneOrBlock(parser);

        return SyntaxTree.While(cond, body).WithRange(keywordToken, iterator.Prev);
    }
}