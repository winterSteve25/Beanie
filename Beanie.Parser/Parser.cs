using ErrFmt;
using Parser.AST;
using Parser.AST.Declarations;
using Parser.Errors;
using Attribute = Parser.AST.Attribute;

namespace Parser;

public class Parser(List<Token> tokens, List<IError> errs)
{
    private int _current = 0;

    private Token Peek() => tokens[_current];
    private Token Previous() => tokens[_current - 1];
    private bool IsAtEnd() => _current >= tokens.Count;

    public NamespaceUnit Parse()
    {
        if (Match(TokenType.Namespace))
        {
            return ParseNamespace();
        }

        var decls = new List<IDeclaration>();

        while (!IsAtEnd())
        {
            decls.Add(ParseDeclaration());
        }

        return new NamespaceUnit(null, null, decls, 0, Previous().End);
    }

    private NamespaceUnit ParseNamespace()
    {
        var namespaceToken = Previous();
        var identifier = ParseIdentifier();
        Consume(TokenType.Semicolon);

        var declarations = new List<IDeclaration>();
        while (!IsAtEnd())
        {
            declarations.Add(ParseDeclaration());
        }

        return new NamespaceUnit(namespaceToken, identifier, declarations, namespaceToken.Start, Previous().End);
    }

    private IDeclaration ParseDeclaration()
    {
        var attributes = ParseAttributes();
        var accessModifier = ParseAccessModifier();

        if (Match(TokenType.Class)) return ParseClassDeclaration(attributes, accessModifier);
        if (Match(TokenType.Enum)) return ParseEnumDeclaration(attributes, accessModifier);
        if (Match(TokenType.Union)) return ParseUnionDeclaration(attributes, accessModifier);
        if (Match(TokenType.Interface)) return ParseInterfaceDeclaration(attributes, accessModifier);
        if (Match(TokenType.Type)) return ParseTypeDeclaration(attributes);
        if (Match(TokenType.Macro)) return ParseMacroDeclaration();

        throw Error(Peek(), "Expected a declaration.");
    }

    private ClassDeclaration ParseClassDeclaration(List<Attribute> attributes, AccessModifier accessModifier)
    {
        var classToken = Previous();
        var identifier = Consume(TokenType.Identifier, "Expected class name.");
        var generic = ParseGeneric();
        var inheritance = ParseInheritance();
        Consume(TokenType.CurlyLeft, "Expected '{' before class body.");
        var body = ParseClassBody();
        var curlyRight = Consume(TokenType.CurlyRight, "Expected '}' after class body.");

        return new ClassDeclaration(attributes, accessModifier, null, null, classToken, identifier, generic,
            inheritance, Previous(), curlyRight, body, classToken.Start, curlyRight.End);
    }

    private Delimited<IClassBodyDeclaration> ParseClassBody()
    {
        var declarations = new List<IClassBodyDeclaration>();
        while (!Check(TokenType.CurlyRight) && !IsAtEnd())
        {
            declarations.Add(ParseClassBodyDeclaration());
        }

        return CreateDelimited(declarations);
    }

    private IClassBodyDeclaration ParseClassBodyDeclaration()
    {
        var attributes = ParseAttributes();
        var accessModifier = ParseAccessModifier();

        if (Match(TokenType.Identifier) && Check(TokenType.ParenLeft))
        {
            return ParseConstructorDeclaration(attributes, accessModifier);
        }

        var type = ParseTypeExpression();
        var identifier = Consume(TokenType.Identifier, "Expected identifier.");

        if (Match(TokenType.ParenLeft))
        {
            return ParseMethodDeclaration(attributes, accessModifier, type, identifier);
        }
        else if (Match(TokenType.CurlyLeft))
        {
            return ParsePropertyDeclaration(attributes, accessModifier, type, identifier);
        }
        else
        {
            return ParseFieldDeclaration(attributes, accessModifier, type, identifier);
        }
    }

    private AccessModifier ParseAccessModifier()
    {
        Token? token = Consume(TokenType.Public, TokenType.Private, TokenType.Protected);
    }

    private List<Attribute> ParseAttributes()
    {
        throw new NotImplementedException();
    }

    private Identifier? ParseIdentifier()
    {
        throw new NotImplementedException();
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    private bool Match(params TokenType[] types)
    {
        if (!types.Any(Check)) return false;
        Advance();
        return true;
    }

    private Token? Consume(params TokenType[] type)
    {
        if (Match(type)) return Advance();
        errs.Add(new UnexpectedTokenError(Peek(), type, null));
        return null;
    }
}