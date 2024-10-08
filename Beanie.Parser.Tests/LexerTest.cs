using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parser.Errors;

namespace Parser.Tests;

[TestClass]
[TestSubject(typeof(Lexer))]
public class LexerTest
{
    [TestMethod]
    public void Number_StartWithDecimal()
    {
        var (tokens, errors) = Lexer.Tokenize(".12920");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 6, 0, TokenType.LiteralNumber, ".12920"), tokens[0]);
    }

    [TestMethod]
    public void Number_NoDecimal()
    {
        var (tokens, errors) = Lexer.Tokenize("3291");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 4, 0, TokenType.LiteralNumber, "3291"), tokens[0]);
    }

    [TestMethod]
    public void Number_Decimals()
    {
        var (tokens, errors) = Lexer.Tokenize("3291.12");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 7, 0, TokenType.LiteralNumber, "3291.12"), tokens[0]);
    }

    [TestMethod]
    public void Number_Consecutive()
    {
        var (tokens, errors) = Lexer.Tokenize("12.34.56");
        Assert.AreEqual(2, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 5, 0, TokenType.LiteralNumber, "12.34"), tokens[0]);
        Assert.AreEqual(new Token(5, 8, 0, TokenType.LiteralNumber, ".56"), tokens[1]);
    }

    [TestMethod]
    public void String()
    {
        var (tokens, errors) = Lexer.Tokenize("\"hallo there!!!\"");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 16, 0, TokenType.LiteralString, "hallo there!!!"), tokens[0]);
    }

    [TestMethod]
    public void String_EscapedQuote()
    {
        var (tokens, errors) = Lexer.Tokenize("""
                                              "hallo there\"!!!"
                                              """);
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 18, 0, TokenType.LiteralString, "hallo there\"!!!"), tokens[0]);
        Assert.AreEqual("\"hallo there\\\"!!!\"".Substring(0, 18), "\"hallo there\\\"!!!\"");
    }

    [TestMethod]
    public void String_EscapedTab()
    {
        int len = "\t".Length;
        Assert.AreEqual(1, len);

        var (tokens, errors) = Lexer.Tokenize("\"hallo th\tere!!!\"");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 17, 0, TokenType.LiteralString, "hallo th\tere!!!"), tokens[0]);
        Assert.AreEqual("\"hallo th\tere!!!\"".Substring(0, 17), "\"hallo th\tere!!!\"");
    }

    [TestMethod]
    public void String_EscapedNewLine()
    {
        var (tokens, errors) = Lexer.Tokenize("\"hallo th\nere!!!\"");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 17, 0, TokenType.LiteralString, "hallo th\nere!!!"), tokens[0]);
        Assert.AreEqual("\"hallo th\nere!!!\"".Substring(0, 17), "\"hallo th\nere!!!\"");
    }

    [TestMethod]
    public void Bool_True()
    {
        var (tokens, errors) = Lexer.Tokenize("true");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 4, 0, TokenType.LiteralBool, true), tokens[0]);
    }

    [TestMethod]
    public void Bool_False()
    {
        var (tokens, errors) = Lexer.Tokenize("false");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 5, 0, TokenType.LiteralBool, false), tokens[0]);
    }

    [TestMethod]
    public void Keywords()
    {
        StringBuilder sb = new StringBuilder();
        List<Token> tokens = new List<Token>();

        int line = 0;
        int index = 0;

        foreach (TokenType t in Enum.GetValuesAsUnderlyingType<TokenType>())
        {
            string kw = t.ToString().ToLower();
            sb.AppendLine(kw);

            tokens.Add(new Token(index, index + kw.Length, line, t, null));

            line++;
            index = sb.Length;

            if (t == TokenType.Return)
            {
                break;
            }
        }

        var (result, errs) = Lexer.Tokenize(sb.ToString());

        Assert.AreEqual(tokens.Count, result.Count);
        Assert.AreEqual(0, errs.Count);
        CollectionAssert.AreEqual(tokens, result);
    }

    [TestMethod]
    public void Ident_Simple()
    {
        var (tokens, errors) = Lexer.Tokenize("myVariable");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 10, 0, TokenType.Identifier, "myVariable"), tokens[0]);
    }

    [TestMethod]
    public void Ident_WithNumbers()
    {
        var (tokens, errors) = Lexer.Tokenize("var123");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 6, 0, TokenType.Identifier, "var123"), tokens[0]);
    }

    [TestMethod]
    public void Ident_WithUnderscore()
    {
        var (tokens, errors) = Lexer.Tokenize("_privateVar");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 11, 0, TokenType.Identifier, "_privateVar"), tokens[0]);
    }

    [TestMethod]
    public void Ident_MultipleIdents()
    {
        var (tokens, errors) = Lexer.Tokenize("firstVar secondVar thirdVar");
        Assert.AreEqual(3, tokens.Count); // 3 idents
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 8, 0, TokenType.Identifier, "firstVar"), tokens[0]);
        Assert.AreEqual(new Token(9, 18, 0, TokenType.Identifier, "secondVar"), tokens[1]);
        Assert.AreEqual(new Token(19, 27, 0, TokenType.Identifier, "thirdVar"), tokens[2]);
    }

    [TestMethod]
    public void Error_MissingClosingStringQuote()
    {
        var (tokens, errors) = Lexer.Tokenize("\"unterminated string");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(new Token(0, 21, 0, TokenType.LiteralString, "unterminated string"), tokens[0]);
        Assert.AreEqual(new UnexpectedEofError(0), errors[0]);
    }

    [TestMethod]
    public void Error_UnknownToken()
    {
        var (tokens, errors) = Lexer.Tokenize("HallOThere $ap");
        Assert.AreEqual(2, tokens.Count);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(new Token(0, 10, 0, TokenType.Identifier, "HallOThere"), tokens[0]);
        Assert.AreEqual(new Token(12, 14, 0, TokenType.Identifier, "ap"), tokens[1]);
        Assert.AreEqual(new UnknownTokenError(new Token(11, 12, 0, TokenType.Unknown, '$')), errors[0]);
    }

    [TestMethod]
    public void GreaterThan()
    {
        var (tokens, errors) = Lexer.Tokenize(">");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.GreaterThan, null), tokens[0]);
    }

    [TestMethod]
    public void LessThan()
    {
        var (tokens, errors) = Lexer.Tokenize("<");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.LessThan, null), tokens[0]);
    }

    [TestMethod]
    public void Equality()
    {
        var (tokens, errors) = Lexer.Tokenize("==");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 2, 0, TokenType.Equality, null), tokens[0]);
    }

    [TestMethod]
    public void GreaterThanEquality()
    {
        var (tokens, errors) = Lexer.Tokenize(">=");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 2, 0, TokenType.GreaterThanEquality, null), tokens[0]);
    }

    [TestMethod]
    public void LessThanEquality()
    {
        var (tokens, errors) = Lexer.Tokenize("<=");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 2, 0, TokenType.LessThanEquality, null), tokens[0]);
    }

    [TestMethod]
    public void NotEqual()
    {
        var (tokens, errors) = Lexer.Tokenize("!=");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 2, 0, TokenType.NotEqual, null), tokens[0]);
    }
    
    [TestMethod]
    public void Bang()
    {
        var (tokens, errors) = Lexer.Tokenize("!");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Bang, null), tokens[0]);
    }

    [TestMethod]
    public void Equals()
    {
        var (tokens, errors) = Lexer.Tokenize("=");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Equals, null), tokens[0]);
    }

    [TestMethod]
    public void Comma()
    {
        var (tokens, errors) = Lexer.Tokenize(",");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Comma, null), tokens[0]);
    }

    [TestMethod]
    public void Dot()
    {
        var (tokens, errors) = Lexer.Tokenize(".");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Dot, null), tokens[0]);
    }

    [TestMethod]
    public void Semicolon()
    {
        var (tokens, errors) = Lexer.Tokenize(";");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Semicolon, null), tokens[0]);
    }

    [TestMethod]
    public void ParenLeft()
    {
        var (tokens, errors) = Lexer.Tokenize("(");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.ParenLeft, null), tokens[0]);
    }

    [TestMethod]
    public void ParenRight()
    {
        var (tokens, errors) = Lexer.Tokenize(")");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.ParenRight, null), tokens[0]);
    }

    [TestMethod]
    public void CurlyLeft()
    {
        var (tokens, errors) = Lexer.Tokenize("{");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.CurlyLeft, null), tokens[0]);
    }

    [TestMethod]
    public void CurlyRight()
    {
        var (tokens, errors) = Lexer.Tokenize("}");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.CurlyRight, null), tokens[0]);
    }

    [TestMethod]
    public void SquareLeft()
    {
        var (tokens, errors) = Lexer.Tokenize("[");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.SquareLeft, null), tokens[0]);
    }

    [TestMethod]
    public void SquareRight()
    {
        var (tokens, errors) = Lexer.Tokenize("]");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.SquareRight, null), tokens[0]);
    }

    [TestMethod]
    public void Arrow()
    {
        var (tokens, errors) = Lexer.Tokenize("=>");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 2, 0, TokenType.Arrow, null), tokens[0]);
    }

    [TestMethod]
    public void Plus()
    {
        var (tokens, errors) = Lexer.Tokenize("+");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Plus, null), tokens[0]);
    }

    [TestMethod]
    public void Minus()
    {
        var (tokens, errors) = Lexer.Tokenize("-");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Minus, null), tokens[0]);
    }

    [TestMethod]
    public void Star()
    {
        var (tokens, errors) = Lexer.Tokenize("*");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Star, null), tokens[0]);
    }

    [TestMethod]
    public void Slash()
    {
        var (tokens, errors) = Lexer.Tokenize("/");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Slash, null), tokens[0]);
    }

    [TestMethod]
    public void Percent()
    {
        var (tokens, errors) = Lexer.Tokenize("%");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 1, 0, TokenType.Percent, null), tokens[0]);
    }

    [TestMethod]
    public void TokenCombinationWithComment()
    {
        var (tokens, errors) = Lexer.Tokenize("public class MyClass // this is a comment\nprivate return;");
        List<Token> expectedTokens =
        [
            new Token(0, 6, 0, TokenType.Public, null),
            new Token(7, 12, 0, TokenType.Class, null),
            new Token(13, 20, 0, TokenType.Identifier, "MyClass"),
            new Token(42, 49, 1, TokenType.Private, null),
            new Token(50, 56, 1, TokenType.Return, null),
            new Token(56, 57, 1, TokenType.Semicolon, null),
        ];

        Assert.AreEqual(0, errors.Count);
        CollectionAssert.AreEqual(expectedTokens, tokens);
    }
}