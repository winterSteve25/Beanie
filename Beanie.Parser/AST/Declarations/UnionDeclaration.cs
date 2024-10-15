namespace Parser.AST.Declarations;

public record UnionDeclaration(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    Token? MacroToken,
    Token UnionToken,
    Token Identifier,
    IGeneric? Generic,
    Inheritance? Inheritance,
    Token CurlyLeft,
    UnionDeclaration.UnionBody Body,
    Token CurlyRight,
    int Start,
    int End
) : IDeclaration
{
    public record UnionBody(
        List<UnionCase> Cases,
        List<MethodDefinition> Methods,
        int Start,
        int End
    ) : IAstElement;

    public record UnionCase(
        Token Identifier,
        Token ParenLeft,
        Delimited<Param> ParamList,
        Token ParenRight,
        Token Semicolon,
        int Start,
        int End
    ) : IAstElement;
}