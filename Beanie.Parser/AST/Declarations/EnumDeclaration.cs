namespace Parser.AST.Declarations;

public record EnumDeclaration(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    Token EnumToken,
    Token Identifier,
    Token ParenLeft,
    Delimited<Param> ParamList,
    Token ParenRight,
    Inheritance? Inheritance,
    Token CurlyLeft,
    EnumDeclaration.EnumBody Body,
    Token CurlyRight,
    int Start,
    int End
) : IDeclaration
{
    public record EnumBody(
        List<EnumCase> Cases,
        List<MethodDefinition> Methods,
        int Start,
        int End
    ) : IAstElement;

    public record EnumCase(
        Token Identifier,
        Token? ParenLeft,
        Delimited<IExpression> ExprList,
        Token? ParenRight,
        Token Semicolon,
        int Start,
        int End
    ) : IAstElement;
}