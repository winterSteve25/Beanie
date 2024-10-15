namespace Parser.AST.Declarations;

public record InterfaceDeclaration(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    Token InterfaceToken,
    Token Identifier,
    IGeneric? Generic,
    Inheritance? Inheritance,
    Token CurlyLeft,
    InterfaceDeclaration.InterfaceBody Body,
    Token CurlyRight,
    int Start,
    int End
) : IDeclaration
{
    public record InterfaceBody(
        List<IInterfaceMember> Members,
        int Start,
        int End
    ) : IAstElement;
}