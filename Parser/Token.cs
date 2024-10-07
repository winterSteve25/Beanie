namespace Parser;

public record Token(
    int Start,
    int End,
    int Line,
    TokenType Type,
    object? TokenData
)
{
    public override string ToString()
    {
        if (TokenData is not null)
        {
            return $"{Type} at {Start}:{End} on line {Line} with data '{TokenData}'";
        }
        
        return $"{Type} at {Start}:{End} on line {Line}";
    }
}

public enum TokenType
{
    Public, // public
    Private, // private
    Protected, //protected
    
    Class, // class
    Sealed, // sealed
    Abstract, // abstract
    Union, // union
    Enum, // enum
    Interface, // interface
    Type, // type
    Namespace, // namespace
    Macro, // macro
    
    This, // this
    Get, // get
    Set, // set
    Match, // match
    If, // if
    For, // for
    While, // while
    StackAlloc, // stackalloc
    Return, // return
    
    Identifier, // ex: hello
    LiteralString, // ex: ""
    LiteralNumber, // ex: 1920
    LiteralBool, // ex: true
    
    GreaterThan, // >
    LessThan, // <
    Equality, // ==
    GreaterThanEquality, // >=
    LessThanEquality, // <=
    NotEqual, // !=
    
    Equals, // =
    Comma, // ,
    Dot, // .
    Semicolon, // ;
    ParenLeft, // (
    ParenRight, // )
    CurlyLeft, // { 
    CurlyRight, // }
    SquareLeft, // [
    SquareRight, // ]
    Arrow, // =>
    Plus, // +
    Minus, // -
    Star, // *
    Slash, // /
    Percent, // %
    
    // following are not produced by lexer
    Unknown,
    NewLine, 
    EndOfFile,
}