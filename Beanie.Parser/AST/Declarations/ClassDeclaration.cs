namespace Parser.AST.Declarations;

public record ClassDeclaration(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    AbstractOrSealed? AbstractOrSealed,
    Token ClassToken,
    Token Identifier,
    IGeneric? Generic,
    Inheritance? Inheritance,
    Token CurlyL,
    Token CurlyR,
    List<IClassBodyDeclaration> Body,
    int Start,
    int End
) : IDeclaration;