﻿using MrKWatkins.Ast.Position;
using Socordia.CodeAnalysis.Core.Attributes;
using System.Reflection;

namespace Socordia.CodeAnalysis.Parsing;

public sealed class TokenIterator
{
    private readonly TextFile _document;
    private readonly List<Token> _tokens;
    public readonly List<Message> Messages = [];

    public TokenIterator(List<Token> tokens, TextFile document)
    {
        _tokens = tokens;
        _document = document;
    }

    public Token Current => Peek(0);
    public int Position { get; set; }
    public Token Prev => Peek(-1);

    public static string GetTokenRepresentation(TokenType kind)
    {
        var field = kind.GetType().GetField(Enum.GetName(kind));

        var lexeme = field.GetCustomAttribute<LexemeAttribute>();
        var keyword = field.GetCustomAttribute<KeywordAttribute>();

        if (lexeme is not null)
        {
            return lexeme.Lexeme;
        }

        if (keyword is not null)
        {
            return keyword.Keyword;
        }

        return Enum.GetName(kind);
    }

    public bool ConsumeIfMatch(TokenType kind)
    {
        var result = false;
        if (IsMatch(kind))
        {
            result = true;
            NextToken();
        }

        return result;
    }

    public bool IsMatch(TokenType kind)
    {
        return Current.Type == kind;
    }

    public bool IsMatch(params TokenType[] kinds)
    {
        var result = false;
        foreach (var kind in kinds)
        {
            result |= IsMatch(kind);
        }

        return result;
    }

    public Token Match(TokenType kind)
    {
        if (Current.Type == kind)
        {
            return NextToken();
        }

        var pos = _document.CreatePosition(Current.Start, Current.Text.Length, Current.Line, Current.Column);

        Messages.Add(
            Message.Error($"Expected '{GetTokenRepresentation(kind)}' but got '{GetTokenRepresentation(Current.Type)}'",
                pos));

        NextToken();

        return Token.Invalid;
    }

    public Token NextToken()
    {
        var current = Current;
        Position++;
        return current;
    }

    public Token Peek(int offset)
    {
        var index = Position + offset;
        if (index >= _tokens.Count)
        {
            return _tokens[_tokens.Count - 1];
        }

        return _tokens[index];
    }
}