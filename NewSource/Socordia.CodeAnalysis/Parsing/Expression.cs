﻿using System.Reflection;
using Loyc;
using Loyc.Syntax;
using Socordia.CodeAnalysis.AST;
using Socordia.CodeAnalysis.AST.Expressions;
using Socordia.CodeAnalysis.Core;
using Socordia.CodeAnalysis.Core.Attributes;

namespace Socordia.CodeAnalysis.Parsing;

public static class Expression
{
    public static readonly Dictionary<TokenType, int> BinaryOperators = new();
    public static readonly Dictionary<TokenType, int> PostUnaryOperators = new();
    public static readonly Dictionary<TokenType, int> PreUnaryOperators = new();

    static Expression()
    {
        var typeValues = (TokenType[])Enum.GetValues(typeof(TokenType));

        foreach (var op in typeValues)
        {
            var attributes = op.GetType().GetField(Enum.GetName(op)!).GetCustomAttributes<OperatorInfoAttribute>(true);

                foreach (var attribute in attributes)
                {
                    if (attribute.IsUnary)
                    {
                        if (attribute.IsPostUnary)
                        {
                            PostUnaryOperators.Add(op, attribute.Precedence);
                        }
                        else
                        {
                            PreUnaryOperators.Add(op, attribute.Precedence);
                        }
                    }
                    else
                    {
                        BinaryOperators.Add(op, attribute.Precedence);
                    }
                }
        }
    }

    public static int GetBinaryOperatorPrecedence(TokenType kind)
    {
        return BinaryOperators.GetValueOrDefault(kind);
    }

    public static AstNode Parse(Parser parser, ParsePointCollection parsePoints = null, int parentPrecedence = 0)
    {
        AstNode left = null;
        var preUnaryOperatorPrecedence = GetPreUnaryOperatorPrecedence(parser.Iterator.Current.Type);

        if (preUnaryOperatorPrecedence != 0 && preUnaryOperatorPrecedence >= parentPrecedence)
        {
            if (IsPreUnary(parser.Iterator.Current.Type))
            {
                var operatorToken = parser.Iterator.NextToken();

                var operand = Parse(parser, parsePoints, preUnaryOperatorPrecedence + 1);

                left = SyntaxTree.Unary(operatorToken.Text, operand, UnaryOperatorKind.Prefix);
            }
        }
        else
        {
            left = parser.ParsePrimary();

            //parsing postunarys for: hello?;
            var postUnaryOperatorPrecedence = GetPostUnaryOperatorPrecedence(parser.Iterator.Current.Type);

            if (postUnaryOperatorPrecedence != 0 && postUnaryOperatorPrecedence >= parentPrecedence)
            {
                if (IsPostUnary(parser.Iterator.Current.Type))
                {
                    var unaryOperatorToken = parser.Iterator.NextToken();

                    left = SyntaxTree.Unary(unaryOperatorToken.Text, left, UnaryOperatorKind.Suffix);
                }
            }
        }

        while (true)
        {
            var precedence = GetBinaryOperatorPrecedence(parser.Iterator.Current.Type);
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            var operatorToken = parser.Iterator.NextToken();
            var right = Parse(parser, parsePoints, precedence);

            left =  new BinaryOperator($"'{operatorToken.Text}", left, right);

            // parsing postunary for: Hello::new()? = false;
            var postUnaryOperatorPrecedence = GetPostUnaryOperatorPrecedence(parser.Iterator.Current.Type);

            if (postUnaryOperatorPrecedence != 0 && postUnaryOperatorPrecedence >= parentPrecedence)
            {
                if (IsPostUnary(parser.Iterator.Current.Type))
                {
                    var unaryOperatorToken = parser.Iterator.NextToken();

                    left = SyntaxTree.Unary(unaryOperatorToken.Text, left, UnaryOperatorKind.Suffix);
                }
            }
        }

        return left;
    }

    public static List<AstNode> ParseList(Parser parser, TokenType terminator, bool consumeTerminator = true)
    {
        return ParsingHelpers.ParseSeperated<ExpressionParser, AstNode>(parser, terminator,
            consumeTerminator: consumeTerminator);
    }

    private static int GetPreUnaryOperatorPrecedence(TokenType kind)
    {
        return PreUnaryOperators.GetValueOrDefault(kind);
    }

    private static int GetPostUnaryOperatorPrecedence(TokenType kind)
    {
        return PostUnaryOperators.GetValueOrDefault(kind);
    }

    private static bool IsPreUnary(TokenType kind)
    {
        return PreUnaryOperators.ContainsKey(kind);
    }

    private static bool IsPostUnary(TokenType kind)
    {
        return PostUnaryOperators.ContainsKey(kind);
    }

    private class ExpressionParser : IParsePoint
    {
        public static AstNode Parse(TokenIterator iterator, Parser parser)
        {
            return Expression.Parse(parser);
        }
    }
}