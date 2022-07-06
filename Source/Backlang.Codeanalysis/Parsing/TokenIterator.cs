﻿using Loyc.Syntax;

namespace Backlang.Codeanalysis.Parsing;

public sealed class TokenIterator
{
    public readonly List<Message> Messages = new();
    private readonly SourceFile<StreamCharSource> _document;
    private readonly List<Token> _tokens;

    public TokenIterator(List<Token> tokens, SourceFile<StreamCharSource> document)
    {
        _tokens = tokens;
        this._document = document;
    }

    public Token Current => Peek(0);
    public int Position { get; set; }
    public Token Prev => Peek(-1);

    public bool IsMatch(TokenType kind)
    {
        return Current.Type == kind;
    }

    public bool IsMatch(params TokenType[] kinds)
    {
        bool result = false;
        foreach (var kind in kinds)
        {
            result |= IsMatch(kind);
        }

        return result;
    }

    public Token Match(TokenType kind)
    {
        if (Current.Type == kind)
            return NextToken();

        Messages.Add(Message.Error(_document, $"Expected {kind} but got {Current.Type}", Current.Line, Current.Column));
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
            return _tokens[_tokens.Count - 1];

        return _tokens[index];
    }
}