using Parser.AST.Expressions;

namespace Parser.AST;

public record Attribute(
    Token SqrL,
    Token SqrR,
    Delimited<Attribute.Body>? ABody,
    int Start,
    int End
) : IAstElement
{
    public record Body(
        Token? At,
        AccessExpr Name,
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