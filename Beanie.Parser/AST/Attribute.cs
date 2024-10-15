namespace Parser.AST;

public record Attribute(
    Token SqrL,
    Token SqrR,
    Delimited<Attribute.AttributeBody> Body,
    int Start,
    int End
) : IAstElement
{
    public record AttributeBody(
        Identifier Name,
        Token? ParenL,
        Token? ParenR,
        Delimited<IExpression>? Expressions,
        int Start,
        int End
    ) : IAstElement;
}