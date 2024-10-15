namespace Parser.AST;

public interface IAstElement
{
    int Start { get; }
    int End { get; }
}