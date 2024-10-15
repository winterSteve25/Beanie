using Parser.AST.Expressions;

namespace Parser.AST.Declarations;

public record PropertyDeclaration(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    TypeExpr Type,
    Token Identifier,
    Token CurlyLeft,
    Token CurlyRight,
    PropertyDeclaration.PropertyGetterSetter GetterOrSetter1,
    Token? Comma,
    PropertyDeclaration.PropertyGetterSetter? GetterOrSetter2,
    int Start,
    int End
) : IClassBodyDeclaration, IInterfaceMember
{
    public record PropertyGetterSetter(
        List<Attribute> Attributes,
        AccessModifier? AccessModifier,
        Token GetOrSetToken,
        Token? EqualToken,
        IExpression? InitialExpression,
        int Start,
        int End
    ) : IAstElement
    {
        public bool IsGetter()
        {
            return GetOrSetToken.Type == TokenType.Get;
        }

        public bool IsSetter()
        {
            return GetOrSetToken.Type == TokenType.Set;
        }
    }
}