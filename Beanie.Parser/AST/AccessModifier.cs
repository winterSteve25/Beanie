namespace Parser.AST;

public record AccessModifier(Token Token) : IAstElement
{
    public int Start => Token.Start;
    public int End => Token.End;
}