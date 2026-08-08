using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Smile.Language;

internal sealed class Lexer
{
    private readonly SourceText _source;
    private readonly DiagnosticBag _diagnostics;
    private int _position;

    public Lexer(SourceText source)
    {
        _source = source;
        _diagnostics = new DiagnosticBag(source);
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.ToArray();

    public IReadOnlyList<SyntaxToken> Lex()
    {
        var tokens = new List<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = LexToken();
            tokens.Add(token);
        }
        while (token.Kind != SyntaxKind.EndOfFileToken);

        return tokens;
    }

    private SyntaxToken LexToken()
    {
        while (Current == ' ' || Current == '\t' || Current == '\f' || Current == '\v')
            _position++;

        var start = _position;

        if (Current == '\0')
            return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, string.Empty);

        if (Current == '\r' || Current == '\n')
        {
            if (Current == '\r' && Peek(1) == '\n')
                _position += 2;
            else
                _position++;

            return Token(SyntaxKind.NewLineToken, start);
        }

        if (Current == '\'')
        {
            _position++;
            while (Current != '\0' && Current != '\r' && Current != '\n')
                _position++;
            return Token(SyntaxKind.CommentToken, start);
        }

        if (char.IsLetter(Current) || Current == '_')
        {
            _position++;
            while (char.IsLetterOrDigit(Current) || Current == '_')
                _position++;

            var text = _source.Substring(start, _position - start);
            return new SyntaxToken(SyntaxFacts.GetKeywordKind(text), start, text);
        }

        if (char.IsDigit(Current))
        {
            _position++;
            while (char.IsDigit(Current))
                _position++;

            var text = _source.Substring(start, _position - start);
            if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                _diagnostics.Report("SML1003", new TextSpan(start, text.Length), "Number literal is outside the signed 64-bit range.");
                value = 0;
            }

            return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
        }

        if (Current == '"')
            return LexString();

        switch (Current)
        {
            case '+':
                _position++;
                return Token(SyntaxKind.PlusToken, start);
            case '-':
                _position++;
                return Token(SyntaxKind.MinusToken, start);
            case '=':
                _position++;
                return Token(SyntaxKind.EqualsToken, start);
            case '<' when Peek(1) == '>':
                _position += 2;
                return Token(SyntaxKind.NotEqualsToken, start);
            case '<' when Peek(1) == '=':
                _position += 2;
                return Token(SyntaxKind.LessOrEqualsToken, start);
            case '>' when Peek(1) == '=':
                _position += 2;
                return Token(SyntaxKind.GreaterOrEqualsToken, start);
            case '<':
                _position++;
                return Token(SyntaxKind.LessToken, start);
            case '>':
                _position++;
                return Token(SyntaxKind.GreaterToken, start);
            case '(':
                _position++;
                return Token(SyntaxKind.OpenParenthesisToken, start);
            case ')':
                _position++;
                return Token(SyntaxKind.CloseParenthesisToken, start);
            case '[':
                _position++;
                return Token(SyntaxKind.OpenBracketToken, start);
            case ']':
                _position++;
                return Token(SyntaxKind.CloseBracketToken, start);
            case ';':
                _position++;
                return Token(SyntaxKind.SemicolonToken, start);
        }

        _position++;
        var badText = _source.Substring(start, 1);
        _diagnostics.Report("SML1001", new TextSpan(start, 1), $"Unknown character '{badText}'.");
        return new SyntaxToken(SyntaxKind.BadToken, start, badText);
    }

    private SyntaxToken LexString()
    {
        var start = _position++;
        var value = new StringBuilder();
        var terminated = false;

        while (Current != '\0' && Current != '\r' && Current != '\n')
        {
            if (Current == '"')
            {
                if (Peek(1) == '"')
                {
                    value.Append('"');
                    _position += 2;
                    continue;
                }

                _position++;
                terminated = true;
                break;
            }

            value.Append(Current);
            _position++;
        }

        if (!terminated)
            _diagnostics.Report("SML1002", new TextSpan(start, Math.Max(1, _position - start)), "Unterminated text literal.");

        return new SyntaxToken(SyntaxKind.StringToken, start, _source.Substring(start, _position - start), value.ToString());
    }

    private char Current => _source[_position];
    private char Peek(int offset) => _source[_position + offset];

    private SyntaxToken Token(SyntaxKind kind, int start) =>
        new(kind, start, _source.Substring(start, _position - start));
}
