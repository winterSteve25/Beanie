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
        Token? At,
        Identifier Name,
        Token? ParenL,
        Token? ParenR,
        Delimited<IExpression>? Expressions,
        int Start,
        int End
    ) : IAstElement
    {
        public bool IsMacro()
        {
            return At is not null;
        }
    }
}