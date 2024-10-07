using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parser.Tests;

[TestClass]
[TestSubject(typeof(Lexer))]
public class LexerTest
{
    [TestMethod]
    public void NumberStartWithDecimal()
    {
        var (tokens, errors) = Lexer.Tokenize(".12920");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 6, 0, TokenType.LiteralNumber, ".12920"), tokens[0]);
    }

    [TestMethod]
    public void NumberNoDecimal()
    {
        var (tokens, errors) = Lexer.Tokenize("3291");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 4, 0, TokenType.LiteralNumber, "3291"), tokens[0]);
    }

    [TestMethod]
    public void NumberDecimals()
    {
        var (tokens, errors) = Lexer.Tokenize("3291.12");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 7, 0, TokenType.LiteralNumber, "3291.12"), tokens[0]);
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
    public void StringEscapedQuote()
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
    public void StringEscapedTab()
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
    public void StringEscapedNewLine()
    {
        var (tokens, errors) = Lexer.Tokenize("\"hallo th\nere!!!\"");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 17, 0, TokenType.LiteralString, "hallo th\nere!!!"), tokens[0]);
        Assert.AreEqual("\"hallo th\nere!!!\"".Substring(0, 17), "\"hallo th\nere!!!\"");
    }

    [TestMethod]
    public void KW_Public()
    {
        var (tokens, errors) = Lexer.Tokenize("public");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 6, 0, TokenType.Public, null), tokens[0]);
    }

    [TestMethod]
    public void KWs()
    {
        var (tokens, errors) = Lexer.Tokenize("public private\nprotected");
        Assert.AreEqual(4, tokens.Count);
        Assert.AreEqual(0, errors.Count);

        List<Token> right =
        [
            new Token(0, 6, 0, TokenType.Public, null),
            new Token(7, 14, 0, TokenType.Private, null),
            new Token(14, 15, 0, TokenType.NewLine, null),
            new Token(15, 24, 1, TokenType.Protected, null),
        ];

        CollectionAssert.AreEqual(right, tokens);
    }
}