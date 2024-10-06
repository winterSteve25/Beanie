using System.Text.RegularExpressions;
using ErrFmt;
using Parser.Errors;

namespace Parser;

public class Lexer
{
    private static readonly Regex Ident = new Regex(@"[A-Za-z_]+[A-Za-z_0-9]*");
    private static readonly Regex Number = new Regex(@"[0-9]");

    private int _pointer;
    private int _line;

    private readonly string[] _source;

    public Lexer(string source)
    {
        _source = source.Split("\n");
        _pointer = 0;
        _line = 0;
    }

    public (List<Token>, List<IError>) Lex()
    {
        var ts = new List<Token>();
        var es = new List<IError>();
        var n = Next(es);
        
        while (n is null || n.Type != TokenType.EndOfFile)
        {
            if (n is not null)
                ts.Add(n);
            n = Next(es);
        }

        return (ts, es);
    }

    private Token? Next(List<IError> errs)
    {
        if (_line >= _source.Length)
        {
            return new Token(_pointer, _pointer, _line, TokenType.EndOfFile, null);
        }

        var line = _source[_line];

        if (_pointer >= line.Length)
        {
            return new Token(_pointer, _pointer, _line, TokenType.EndOfFile, null);
        }

        var ptrChar = line[_pointer];

        Token? t = ptrChar switch
        {
            'p' => MatchesWord(line, TokenType.Public) ??
                   MatchesWord(line, TokenType.Private) ??
                   MatchesWord(line, TokenType.Protected),
            'c' => MatchesWord(line, TokenType.Class),
            's' => MatchesWord(line, TokenType.Set) ??
                   MatchesWord(line, TokenType.Sealed) ??
                   MatchesWord(line, TokenType.StackAlloc),
            'a' => MatchesWord(line, TokenType.Abstract),
            'u' => MatchesWord(line, TokenType.Union),
            'e' => MatchesWord(line, TokenType.Enum),
            'i' => MatchesWord(line, TokenType.Interface) ??
                   MatchesWord(line, TokenType.If),
            't' => MatchesWord(line, TokenType.This) ??
                   MatchesWord(line, TokenType.Type) ??
                   MatchesWord(line, "true", TokenType.LiteralBool, true),
            'n' => MatchesWord(line, TokenType.Namespace),
            'm' => MatchesWord(line, TokenType.Match) ??
                   MatchesWord(line, TokenType.Macro),
            'g' => MatchesWord(line, TokenType.Get),
            'f' => MatchesWord(line, TokenType.For) ??
                   MatchesWord(line, "false", TokenType.LiteralBool, false),
            'w' => MatchesWord(line, TokenType.While),
            'r' => MatchesWord(line, TokenType.Return),
            '>' => PeekNoWs() == '='
                ? new Token(_pointer, _pointer += 2, _line, TokenType.GreaterThanEquality, null)
                : new Token(_pointer, _pointer += 1, _line, TokenType.GreaterThan, null),
            '<' => PeekNoWs() == '='
                ? new Token(_pointer, _pointer += 2, _line, TokenType.LessThanEquality, null)
                : new Token(_pointer, _pointer += 1, _line, TokenType.LessThan, null),
            '=' => PeekNoWs() == '='
                ? new Token(_pointer, _pointer += 2, _line, TokenType.Equality, null)
                : PeekNoWs() == '>'
                    ? new Token(_pointer, _pointer += 2, _line, TokenType.Arrow, null)
                    : new Token(_pointer, _pointer += 1, _line, TokenType.Equals, null),
            '!' => PeekNoWs() == '='
                ? new Token(_pointer, _pointer += 2, _line, TokenType.NotEqual, null)
                : null,
            ',' => new Token(_pointer, _pointer += 1, _line, TokenType.Comma, null),
            '.' => Number.IsMatch(PeekNoWs().ToString()!)
                ? ConsumeAsNumber()
                : new Token(_pointer, _pointer += 1, _line, TokenType.Dot, null),
            ';' => new Token(_pointer, _pointer += 1, _line, TokenType.Semicolon, null),
            '(' => new Token(_pointer, _pointer += 1, _line, TokenType.ParenLeft, null),
            ')' => new Token(_pointer, _pointer += 1, _line, TokenType.ParenRight, null),
            '{' => new Token(_pointer, _pointer += 1, _line, TokenType.CurlyLeft, null),
            '}' => new Token(_pointer, _pointer += 1, _line, TokenType.CurlyRight, null),
            '[' => new Token(_pointer, _pointer += 1, _line, TokenType.SquareLeft, null),
            ']' => new Token(_pointer, _pointer += 1, _line, TokenType.SquareRight, null),
            '+' => new Token(_pointer, _pointer += 1, _line, TokenType.Plus, null),
            '-' => new Token(_pointer, _pointer += 1, _line, TokenType.Minus, null),
            '*' => PeekNoWs() == '/'
                ? new Token(_pointer, _pointer += 2, _line, TokenType.CommentEnd, null)
                : new Token(_pointer, _pointer += 1, _line, TokenType.Star, null),
            '/' => PeekNoWs() == '/'
                ? new Token(_pointer, _pointer += 2, _line, TokenType.Comment, null)
                : PeekNoWs() == '*'
                    ? new Token(_pointer, _pointer += 2, _line, TokenType.CommentStart, null)
                    : new Token(_pointer, _pointer += 1, _line, TokenType.Slash, null),
            '"' => ConsumeAsString(),
            _ => null,
        };

        if (t is null)
        {
            if (Number.IsMatch(ptrChar.ToString()))
            {
                t = ConsumeAsNumber();
            }
            else if (Ident.IsMatch(ptrChar.ToString()))
            {
                var match = Ident.Match(line, _pointer);
                var ident = match.Value;
                t = new Token(_pointer, _pointer += ident.Length, _line, TokenType.Identifier, ident);
            }
        }

        _pointer++;
        var ptrCopy = _pointer;
        if (_pointer >= line.Length && (_line + 1) < _source.Length)
        {
            _line++;
            _pointer = 0;
        }

        if (t is null)
        {
            var nextWs = line.AsSpan(ptrCopy).IndexOfAny(' ', '\t');
            var end = nextWs == -1 ? line.Length : nextWs;
            errs.Add(new UnknownTokenError(new Token(ptrCopy, end, _line, TokenType.Unknown,
                line.AsSpan(ptrCopy, end - ptrCopy).ToString())));
            return null;
        }
        
        return t;

        Token? ConsumeAsString()
        {
            int startOfStr = _pointer;

            while (PeekNoWs(false) != '"')
            {
                _pointer++;
            }

            _pointer++;
            return new Token(startOfStr, _pointer, _line, TokenType.LiteralString,
                line.Substring(startOfStr, _pointer - startOfStr));
        }

        Token? ConsumeAsNumber()
        {
            int startOfNum = _pointer;

            while (Number.IsMatch(PeekNoWs(false).ToString()!))
            {
                _pointer++;
            }

            return new Token(startOfNum, _pointer, _line, TokenType.LiteralNumber,
                line.Substring(startOfNum, _pointer - startOfNum));
        }
    }

    private char? PeekNoWs(bool moveLines = true)
    {
        int prevPtr = _pointer;
        int prevLine = _line;

        char? c = Peek();

        while (c is ' ' or '\t')
        {
            c = Peek();
        }

        _pointer = prevPtr;
        _line = prevLine;

        return c;

        char? Peek()
        {
            var pointerOverflow = (_pointer + 1) >= _source[_line].Length;
            if (!pointerOverflow) return _source[_line][_pointer + 1];

            if (!moveLines)
            {
                return null;
            }

            if ((_line + 1) >= _source.Length)
            {
                return null;
            }

            _pointer = 0;
            _line++;

            return _source[_line][_pointer];
        }
    }

    private Token? MatchesWord(string line, TokenType tokenType, object? data = null)
    {
        return MatchesWord(line, tokenType.ToString(), tokenType, data);
    }

    private Token? MatchesWord(string line, string word, TokenType tokenType, object? data = null)
    {
        if (!line.AsSpan(_pointer).StartsWith(word)) return null;
        var tok = new Token(_pointer, _pointer + word.Length, _line, tokenType, data);
        _pointer += word.Length;
        return tok;
    }
}