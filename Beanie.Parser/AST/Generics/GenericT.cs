namespace Parser.AST.Generics;

public record GenericT(Token Ident) : IAstElement
{
    public int Start => Ident.Start;
    public int End => Ident.End;
}