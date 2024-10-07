using System.Text;
using System.Text.RegularExpressions;
using ErrFmt;
using Parser.Errors;

namespace Parser;

public class Lexer
{
    private static readonly Regex Ident = new Regex(@"[A-Za-z_0-9]");
    private static readonly Regex Number = new Regex(@"[0-9.]");

    public static (List<Token>, List<IError>) Tokenize(string source)
    {
        var ts = new List<Token>();
        var es = new List<IError>();
        var n = Next(source, 0, 0, es);

        while (n.Item1 is null || n.Item1.Type != TokenType.EndOfFile)
        {
            if (n.Item1 is not null && n.Item1.Type != TokenType.NewLine)
                ts.Add(n.Item1);
            n = Next(source, n.Item2, n.Item3, es);
        }

        return (ts, es);
    }

    private static (Token?, int, int) Next(string source, int i, int line, List<IError> errs)
    {
        if (i >= source.Length)
        {
            return (new Token(i, i, line, TokenType.EndOfFile, null), i, line);
        }

        var c = source[i];

        (Token?, int, int)? token = c switch
        {
            '\n' => (new Token(i, i + 1, line, TokenType.NewLine, null), i + 1, line + 1),
            '\r' => (null, i + 1, line)!,
            'p' => MatchesTokenName(TokenType.Public) ??
                   MatchesTokenName(TokenType.Private) ??
                   MatchesTokenName(TokenType.Protected),
            'c' => MatchesTokenName(TokenType.Class),
            's' => MatchesTokenName(TokenType.Set) ??
                   MatchesTokenName(TokenType.Sealed) ??
                   MatchesTokenName(TokenType.StackAlloc),
            'a' => MatchesTokenName(TokenType.Abstract),
            'u' => MatchesTokenName(TokenType.Union),
            'e' => MatchesTokenName(TokenType.Enum),
            'i' => MatchesTokenName(TokenType.Interface) ??
                   MatchesTokenName(TokenType.If),
            't' => MatchesTokenName(TokenType.This) ??
                   MatchesTokenName(TokenType.Type) ??
                   MatchesWord("true", TokenType.LiteralBool, true),
            'n' => MatchesTokenName(TokenType.Namespace),
            'm' => MatchesTokenName(TokenType.Match) ??
                   MatchesTokenName(TokenType.Macro),
            'g' => MatchesTokenName(TokenType.Get),
            'f' => MatchesTokenName(TokenType.For) ??
                   MatchesWord("false", TokenType.LiteralBool, false),
            'w' => MatchesTokenName(TokenType.While),
            'r' => MatchesTokenName(TokenType.Return),
            ' ' => (null, i + 1, line)!,
            '\t' => (null, i + 1, line)!,
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
                : null,
            ',' => TokenOfLen(1, TokenType.Comma),
            '.' => IsMatch(i + 1, x => Number.IsMatch(x.ToString()), false)
                ? TokenizeNumber(true)
                : TokenOfLen(1, TokenType.Dot),
            ';' => TokenOfLen(1, TokenType.Semicolon),
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
            _ => null,
        };

        if (token is null)
        {
            if (Number.IsMatch(c.ToString()))
            {
                token = TokenizeNumber(false);
            }
            else if (Ident.IsMatch(c.ToString()))
            {
                token = TokenizeIdent();
            }
        }

        // ReSharper disable once InvertIf
        if (token is null)
        {
            errs.Add(new UnknownTokenError(new Token(i, i + 1, line, TokenType.Unknown, c)));
            return (null, i + 1, line);
        }

        return token.Value;

        #region helpers

        (Token, int, int) TokenOfLen(int len, TokenType type, object? data = null)
        {
            return (new Token(i, i + len, line, type, data), i + len, line);
        }

        (Token, int, int)? MatchesTokenName(TokenType tokenType, object? data = null)
        {
            return MatchesWord(tokenType.ToString().ToLower(), tokenType, data);
        }

        (Token, int, int)? MatchesWord(string word, TokenType tokenType, object? data = null)
        {
            if (!source.AsSpan(i).StartsWith(word)) return null;
            var tok = new Token(i, i + word.Length, line, tokenType, data);
            return (tok, i + word.Length, line);
        }

        bool IsMatch(int i, Func<char, bool> predicate, bool errIfEnd)
        {
            if (i >= source.Length)
            {
                if (errIfEnd)
                    errs.Add(new UnexpectedEofError(line));
                return false;
            }

            return predicate(source[i]);
        }

        char? Peek()
        {
            if (i + 1 >= source.Length)
            {
                return null;
            }

            return source[i + 1];
        }

        (Token, int, int) TokenizeNumber(bool hadDecimal)
        {
            int newI = i + 1;

            while (IsMatch(newI, x => Number.IsMatch(x.ToString()), false)) {

                // reached a dot
                if (IsMatch(newI, x => x == '.', false))
                {
                    // if the dot is at the end of the number
                    // and is followed by a possible ident
                    // ignore the dot
                    if (IsMatch(newI + 1, x =>
                        {
                            var s = x.ToString();
                            return !Number.IsMatch(s) && Ident.IsMatch(s);
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

            return (new Token(i, newI, line, TokenType.LiteralNumber, source.Substring(i, newI - i)), newI, line);
        }

        (Token, int, int) TokenizeString()
        {
            StringBuilder sb = new StringBuilder();
            int newI = i + 1;
            
            while (IsMatch(newI, x => x != '"', true))
            {
                // escaped character so immediately consume it
                if (source[newI] == '\\')
                {
                    if (IsMatch(newI + 1, x => x != '"', true))
                    {
                        sb.Append(source[newI]);
                    }
                    
                    newI++;
                }
                
                sb.Append(source[newI]);
                newI++;
            }
            
            // consume the ending "
            newI++;
            string complete = sb.ToString();
            
            return (new Token(i, newI, line, TokenType.LiteralString, complete), newI, line);
        }
        
        (Token, int, int) TokenizeIdent()
        {
            int newI = i + 1;
            
            while (IsMatch(newI, x => Ident.IsMatch(x.ToString()), false))
            {
                newI++;
            }
            
            return (new Token(i, newI, line, TokenType.Identifier, source.Substring(i, newI - i)), newI, line);
        }

        (Token, int, int) SkipComment()
        {
            return (null, source.AsSpan(i).IndexOf('\n') + 1 + i, line + 1)!;
        }

        #endregion
    }
}