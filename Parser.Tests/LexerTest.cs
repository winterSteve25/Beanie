using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Parser.Tests;

[TestClass]
[TestSubject(typeof(Lexer))]
public class LexerTest
{
    [TestMethod]
    public void Number()
    {
        var lexer = new Lexer("12920");
        var (tokens, errors) = lexer.Lex();
        
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 5, 0, TokenType.LiteralNumber, "12920"), tokens[0]);
    }
    
    [TestMethod]
    public void String()
    {
        var lexer = new Lexer("\"hallo there!!!\"");
        var (tokens, errors) = lexer.Lex();
        
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 16, 0, TokenType.LiteralString, "hallo there!!!"), tokens[0]);
    }

    [TestMethod]
    public void KW_Public()
    {
        var lexer = new Lexer("public");
        var (tokens, errors) = lexer.Lex();
        
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(new Token(0, 16, 0, TokenType.Public, null), tokens[0]);
    }
}
