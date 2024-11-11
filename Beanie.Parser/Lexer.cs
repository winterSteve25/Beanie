using System.Text;
using System.Text.RegularExpressions;
using ErrFmt;
using Parser.Errors;

namespace Parser;

public partial class Lexer
{
    private static readonly Regex IdentRegex = IdentRegexGen();
    private static readonly Regex NumberRegex = NumberRegexGen();

    private int _i;
    private int _line;

    private readonly string _source;
    private readonly List<IError> _errs;

    private Lexer(string source)
    {
        _source = source;
        _errs = new List<IError>();
        _i = 0;
        _line = 0;
    }

    public static (List<Token>, List<IError>) Tokenize(string source)
    {
        var ts = new List<Token>();
        var lexer = new Lexer(source);
        var n = lexer.Next();

        while (n.Item1 is null || n.Item1.Type != TokenType.EndOfFile)
        {
            if (n.Item1 is not null && n.Item1.Type != TokenType.NewLine)
                ts.Add(n.Item1);
            lexer._i = n.Item2;
            lexer._line = n.Item3;
            n = lexer.Next();
        }

        return (ts, lexer._errs);
    }

    private (Token?, int, int) Next()
    {
        if (_i >= _source.Length)
        {
            return (new Token(_i, _i, _line, TokenType.EndOfFile, null), _i, _line);
        }

        var c = _source[_i];

        (Token?, int, int)? token = c switch
        {
            '\n' => (new Token(_i, _i + 1, _line, TokenType.NewLine, null), _i + 1, _line + 1),
            '\r' => (null, _i + 1, _line)!,
            'p' => MatchesTokenName(TokenType.Public) ??
                   MatchesTokenName(TokenType.Private) ??
                   MatchesTokenName(TokenType.Protected),
            'c' => MatchesTokenName(TokenType.Class),
            's' => MatchesTokenName(TokenType.Set),
            'u' => MatchesTokenName(TokenType.Union),
            'e' => MatchesTokenName(TokenType.Enum) ??
                   MatchesTokenName(TokenType.Else),
            'i' => MatchesTokenName(TokenType.Interface) ??
                   MatchesTokenName(TokenType.If),
            't' => MatchesTokenName(TokenType.This) ??
                   MatchesTokenName(TokenType.Type) ??
                   MatchesWord("true", TokenType.LiteralBool, true),
            'n' => MatchesTokenName(TokenType.New) ??
                   MatchesTokenName(TokenType.Namespace),
            'm' => MatchesTokenName(TokenType.Match),
            'g' => MatchesTokenName(TokenType.Get),
            'f' => MatchesTokenName(TokenType.For) ??
                   MatchesWord("false", TokenType.LiteralBool, false),
            'w' => MatchesTokenName(TokenType.While),
            'r' => MatchesTokenName(TokenType.Return),
            'd' => MatchesTokenName(TokenType.Defer),
            ' ' => (null, _i + 1, _line)!,
            '\t' => (null, _i + 1, _line)!,
            '>' => Peek() == '='
                ? TokenOfLen(2, TokenType.GreaterThanEquality)
                : TokenOfLen(1, TokenType.GreaterThan),
            '<' => Peek() == '='
                ? TokenOfLen(2, TokenType.LessThanEquality)
                : TokenOfLen(1, TokenType.LessThan),
            '=' => Peek() == '='
                ? TokenOfLen(2, TokenType.Equality)
                : Peek() == '>'
                    ? TokenOfLen(2, TokenType.Arrow)
                    : TokenOfLen(1, TokenType.Equals),
            '!' => Peek() == '='
                ? TokenOfLen(2, TokenType.NotEqual)
                : TokenOfLen(1, TokenType.Bang),
            '|' => Peek() == '|'
                ? TokenOfLen(2, TokenType.Or)
                : TokenOfLen(1, TokenType.Pipe),
            '&' => Peek() == '&'
                ? TokenOfLen(2, TokenType.And)
                : TokenOfLen(1, TokenType.Ampersand),
            ',' => TokenOfLen(1, TokenType.Comma),
            '.' => IsMatch(_i + 1, x => NumberRegex.IsMatch(x.ToString()), false)
                ? TokenizeNumber(true)
                : TokenOfLen(1, TokenType.Dot),
            ';' => TokenOfLen(1, TokenType.Semicolon),
            ':' => TokenOfLen(1, TokenType.Colon),
            '(' => TokenOfLen(1, TokenType.ParenLeft),
            ')' => TokenOfLen(1, TokenType.ParenRight),
            '{' => TokenOfLen(1, TokenType.CurlyLeft),
            '}' => TokenOfLen(1, TokenType.CurlyRight),
            '[' => TokenOfLen(1, TokenType.SquareLeft),
            ']' => TokenOfLen(1, TokenType.SquareRight),
            '+' => TokenOfLen(1, TokenType.Plus),
            '-' => TokenOfLen(1, TokenType.Minus),
            '*' => TokenOfLen(1, TokenType.Star),
            '/' => Peek() == '/'
                ? SkipComment()
                : TokenOfLen(1, TokenType.Slash),
            '%' => TokenOfLen(1, TokenType.Percent),
            '"' => TokenizeString(),
            '@' => Peek() == '{'
                ? TokenizeCodeBlock()
                : TokenOfLen(1, TokenType.At),
            '_' => Peek() is null || !IdentRegex.IsMatch(Peek()!.Value.ToString())
                ? TokenOfLen(1, TokenType.Underscore)
                : null,
            _ => null,
        };

        if (token is null)
        {
            if (NumberRegex.IsMatch(c.ToString()))
            {
                token = TokenizeNumber(false);
            }
            else if (IdentRegex.IsMatch(c.ToString()))
            {
                token = TokenizeIdent();
            }
        }

        // ReSharper disable once InvertIf
        if (token is null)
        {
            _errs.Add(new UnknownTokenError(new Token(_i, _i + 1, _line, TokenType.Unknown, c)));
            return (null, _i + 1, _line);
        }

        return token.Value;
    }


    (Token, int, int) TokenOfLen(int len, TokenType type, object? data = null)
    {
        return (new Token(_i, _i + len, _line, type, data), _i + len, _line);
    }

    (Token, int, int)? MatchesTokenName(TokenType tokenType, object? data = null)
    {
        return MatchesWord(tokenType.ToString().ToLower(), tokenType, data);
    }

    (Token, int, int)? MatchesWord(string word, TokenType tokenType, object? data = null)
    {
        if (!_source.AsSpan(_i).StartsWith(word)) return null;
        var tok = new Token(_i, _i + word.Length, _line, tokenType, data);
        return (tok, _i + word.Length, _line);
    }

    bool IsMatch(int i, Func<char, bool> predicate, bool errIfEnd)
    {
        if (i >= _source.Length)
        {
            if (errIfEnd)
                _errs.Add(new UnexpectedEofError());
            return false;
        }

        return predicate(_source[i]);
    }

    char? Peek()
    {
        if (_i + 1 >= _source.Length)
        {
            return null;
        }

        return _source[_i + 1];
    }

    (Token, int, int) TokenizeNumber(bool hadDecimal)
    {
        int newI = _i + 1;

        while (IsMatch(newI, x => NumberRegex.IsMatch(x.ToString()), false))
        {
            // reached a dot
            if (IsMatch(newI, x => x == '.', false))
            {
                // if the dot is at the end of the number
                // and is followed by a possible ident
                // ignore the dot
                if (IsMatch(newI + 1, x =>
                    {
                        var s = x.ToString();
                        return !NumberRegex.IsMatch(s) && IdentRegex.IsMatch(s);
                    }, false))
                {
                    break;
                }

                // if started with decimal then this dot doesn't make sense as a decimal so we end here
                if (hadDecimal)
                {
                    break;
                }

                hadDecimal = true;
            }

            newI += 1;
        }

        return (new Token(_i, newI, _line, TokenType.LiteralNumber, _source.Substring(_i, newI - _i)), newI, _line);
    }

    (Token, int, int) TokenizeString()
    {
        StringBuilder sb = new StringBuilder();
        int newI = _i + 1;

        while (IsMatch(newI, x => x != '"', true))
        {
            // escaped character so immediately consume it
            if (_source[newI] == '\\')
            {
                if (IsMatch(newI + 1, x => x != '"', true))
                {
                    sb.Append(_source[newI]);
                }

                newI++;
            }

            sb.Append(_source[newI]);
            newI++;
        }

        // consume the ending "
        newI++;
        string complete = sb.ToString();

        return (new Token(_i, newI, _line, TokenType.LiteralString, complete), newI, _line);
    }

    (Token, int, int) TokenizeIdent()
    {
        int newI = _i + 1;

        while (IsMatch(newI, x => IdentRegex.IsMatch(x.ToString()), false))
        {
            newI++;
        }

        return (new Token(_i, newI, _line, TokenType.Identifier, _source.Substring(_i, newI - _i)), newI, _line);
    }

    (Token, int, int) TokenizeCodeBlock()
    {
        var end = _i + 2;
        var startLine = _line;

        while (true)
        {
            if (end >= _source.Length)
            {
                end = _source.Length;
                _errs.Add(new UnexpectedEofError());
                return (new Token(_i, end, startLine, TokenType.CodeBlock, _source.Substring(_i + 2, end - _i - 2)),
                    end, _line)!;
            }

            if (_source[end] == '\n')
            {
                _line++;
            }

            if (_source[end] == '}' && _source[end + 1] == '@')
            {
                end += 2;
                break;
            }

            end++;
        }

        return (new Token(_i, end, startLine, TokenType.CodeBlock, _source.Substring(_i + 2, end - _i - 4)), end,
            _line)!;
    }

    (Token, int, int) SkipComment()
    {
        return (null, _source.AsSpan(_i).IndexOf('\n') + 1 + _i, _line + 1)!;
    }

    [GeneratedRegex(@"[A-Za-z_0-9]")]
    private static partial Regex IdentRegexGen();

    [GeneratedRegex(@"[0-9.]")]
    private static partial Regex NumberRegexGen();
}