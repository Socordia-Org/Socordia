using System.Reflection;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Socordia.CodeAnalysis.Core;
using Socordia.CodeAnalysis.Core.Attributes;

namespace Socordia.CodeAnalysis.Parsing;

public sealed class Lexer : BaseLexer
{
    private static readonly Dictionary<string, TokenType> SymbolTokens = new(StringComparer.Ordinal);

    static Lexer()
    {
        var typeValues = (TokenType[])Enum.GetValues(typeof(TokenType));

        foreach (var op in typeValues)
        {
            var attributes = op.GetType()
                .GetField(Enum.GetName(op)).GetCustomAttributes<LexemeAttribute>(true);

            if (attributes != null && attributes.Any())
            {
                foreach (var attribute in attributes)
                {
                    SymbolTokens.Add(attribute.Lexeme, op);
                }
            }
        }

        SymbolTokens = new Dictionary<string, TokenType>(SymbolTokens.OrderByDescending(_ => _.Key.Length));
    }

    protected override Token NextToken()
    {
        if (IsMatch("///"))
        {
            return LexDocComment();
        }

        SkipWhitespaces();
        SkipComments();

        if (_position >= _document.Text.Length)
        {
            return new Token(TokenType.EOF, "\0", _position, _position, _line, _column);
        }

        if (Current() == '\'')
        {
            return LexCharLiteral();
        }

        if (Current() == '"')
        {
            return LexDoubleQuoteString();
        }

        if (IsMatch("0x"))
        {
            return LexHexNumber();
        }

        if (IsMatch("0b"))
        {
            return LexBinaryNumber();
        }

        if (char.IsDigit(Current()))
        {
            return LexDecimalNumber();
        }

        if (IsIdentifierStartDigit())
        {
            var identifier = LexIdentifier();

            if (IsOperatorIdentifier(identifier))
            {
                identifier.Type = GetOperatorKind(identifier);
            }

            return identifier;
        }

        foreach (var symbol in SymbolTokens)
        {
            if (IsMatch(symbol.Key))
            {
                return LexSymbol(symbol);
            }
        }

        ReportError();

        return Token.Invalid;
    }

    private Token LexDocComment()
    {
        var oldPos = _position;
        var contentBuilder = new StringBuilder();

        do
        {
            contentBuilder.AppendLine(ReadLine().TrimStart('/').Trim());
        } while (IsMatch("///"));

        return new Token(TokenType.DocComment, contentBuilder.ToString(), oldPos, _position, _line, _column);
    }


    private string ReadLine()
    {
        var sb = new StringBuilder();

        while (Current() != '\n')
        {
            Advance();
            _column++;
        }

        Advance();
        _column = 0;
        _line++;

        return sb.ToString();
    }

    private static bool IsBinaryDigit(char c)
    {
        return c == '0' || c == '1';
    }

    private static bool IsHex(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }

    private TokenType GetOperatorKind(Token identifier)
    {
        foreach (var s in SymbolTokens)
        {
            if (identifier.Text == s.Key)
            {
                return s.Value;
            }
        }

        return TokenType.Identifier;
    }

    private bool IsIdentifierDigit()
    {
        return char.IsLetterOrDigit(Current()) || Current() == '_';
    }

    private bool IsIdentifierStartDigit()
    {
        return char.IsLetter(Current()) || Current() == '_';
    }

    private bool IsMatch(string token)
    {
        var result = Current() == token[0];

        for (var i = 1; i < token.Length; i++)
        {
            if (result)
            {
                result = result && Peek(i) == token[i];
            }
        }

        return result;
    }

    private bool IsOperatorIdentifier(Token identifier)
    {
        return SymbolTokens.ContainsKey(identifier.Text);
    }

    private Token LexBinaryNumber()
    {
        _position += 2;
        _column += 2;

        var oldpos = _position;
        var oldcolumn = _column;

        while (IsBinaryDigit(Current()) || Current() == '_')
        {
            Advance();
            _column++;
        }

        return new Token(TokenType.BinNumber,
            _document.Text.Slice(oldpos, _position - oldpos).ToString().Replace("_", string.Empty), oldpos, _position,
            _line, oldcolumn);
    }

    private Token LexCharLiteral()
    {
        var oldpos = ++_position;
        var oldColumn = _column;

        if (Current() == '\n' || Current() == '\r')
        {
            var range = _document.CreatePosition(_position, 1, _line, _column);
            Messages.Add(Message.Error("Unterminated Char-Literal", range));

            return Token.Invalid;
        }

        while (Peek() != '\'' && Peek() != '\0')
        {
            Advance();
            _column++;
        }

        _column += 2;

        return new Token(TokenType.CharLiteral, _document.Text.Slice(oldpos, _position - oldpos).ToString(), oldpos - 1,
            ++_position, _line, oldColumn);
    }

    private Token LexDecimalNumber()
    {
        var oldpos = _position;
        var oldcolumn = _column;

        while (char.IsDigit(Current()))
        {
            _position++;
            _column++;
        }

        if (char.IsDigit(Peek(1)) && Current() == '.')
        {
            Advance();
            _column++;

            while (char.IsDigit(Current()))
            {
                Advance();
                _column++;
            }
        }

        return new Token(TokenType.Number, _document.Text.Slice(oldpos, _position - oldpos).ToString(), oldpos,
            _position, _line, oldcolumn);
    }

    private Token LexDoubleQuoteString()
    {
        var oldpos = ++_position;
        var oldColumn = _column;

        while (Peek() != '"' && Peek() != '\0')
        {
            if (Current() == '\n' || Current() == '\r')
            {
                var range = _document.CreatePosition(_position, 1, _line, _column);
                Messages.Add(Message.Error("Unterminated String-Literal", range));

                return new Token(TokenType.StringLiteral,
                    _document.Text.Slice(oldpos, _position - oldpos - 1).ToString(), oldpos - 1, _position, _line,
                    oldColumn);
            }

            Advance();
            _column++;
        }

        _column += 2;

        return new Token(TokenType.StringLiteral, _document.Text.Slice(oldpos, _position - oldpos).ToString(),
            oldpos - 1, ++_position, _line, oldColumn);
    }

    private Token LexHexNumber()
    {
        _position += 2;
        _column += 2;

        var oldpos = _position;
        var oldcolumn = _column;

        while (IsHex(Current()) || Current() == '_')
        {
            Advance();
            _column++;
        }

        return new Token(TokenType.HexNumber,
            _document.Text.Slice(oldpos, _position - oldpos).ToString().Replace("_", string.Empty), oldpos, _position,
            _line, oldcolumn);
    }

    private Token LexIdentifier()
    {
        var oldpos = _position;

        var oldcolumn = _column;
        while (IsIdentifierDigit())
        {
            Advance();
            _column++;
        }

        var tokenText = _document.Text.Slice(oldpos, _position - oldpos).ToString();

        return new Token(TokenUtils.GetTokenType(tokenText), tokenText, oldpos, _position, _line, oldcolumn);
    }

    private Token LexSymbol(KeyValuePair<string, TokenType> symbol)
    {
        var oldpos = _position;

        _position += symbol.Key.Length;
        _column += symbol.Key.Length;

        var text = _document.Text.Substring(oldpos, symbol.Key.Length);

        return new Token(SymbolTokens[text], text, oldpos, _position, _line, _column);
    }

    private void SkipComments()
    {
        while (IsMatch("/*") || IsMatch("//"))
        {
            if (IsMatch("//"))
            {
                Advance();
                Advance();
                _column++;
                _column++;

                while (Current() != '\n' && Current() != '\r' && Current() != '\0')
                {
                    Advance();
                    _column++;
                }

                if (Current() == '\n' || Current() == '\r')
                {
                    Advance();
                    _column++;
                }

                SkipWhitespaces();
            }
            else if (IsMatch("/*"))
            {
                var oldpos = _position;
                var oldLine = _line;
                var oldColumn = _column;

                Advance();
                Advance();
                _column++;
                _column++;

                while (!IsMatch("*/"))
                {
                    if (Current() == '\0')
                    {
                        break;
                    }

                    Advance();
                    _column++;
                }

                if (IsMatch("*/"))
                {
                    Advance();
                    _column++;

                    Advance();
                    _column++;
                }
                else
                {
                    var pos = _document.CreatePosition(oldpos, _position, oldLine, oldColumn);
                    Messages.Add(Message.Error("Multiline comment is not closed", pos));

                    return;
                }

                SkipWhitespaces();
            }
        }
    }

    private void SkipWhitespaces()
    {
        while (char.IsWhiteSpace(Current()) && _position <= _document.Text.Length)
        {
            if (Current() == '\r')
            {
                _line++;
                _column = 1;
                Advance();

                if (Current() == '\n')
                {
                    Advance();
                }
            }
            else
            {
                Advance();
                _column++;
            }
        }
    }
}