using Parser.AST.Expressions;

namespace Parser.AST.Declarations;

public record TypeDeclaration(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    Token TypeToken,
    Token Identifier,
    Token EqualsToken,
    TypeExpr TypeExpr,
    Token SemicolonToken,
    int Start,
    int End
) : IDeclaration;