using ErrFmt;
using Parser.AST;
using Parser.AST.Declarations;
using Parser.AST.Expressions;
using Parser.AST.Generics;
using Parser.AST.Statements;
using Parser.Errors;
using Attribute = Parser.AST.Attribute;

namespace Parser;

public class Parser
{
    private int _current;

    private List<Token> Tokens { get; }
    public List<IError> Errs { get; }

    public Parser(List<Token> tokens, List<IError> errs)
    {
        Tokens = tokens;
        Errs = errs;
    }

    private Token Peek() => Tokens[_current];
    private Token Previous() => Tokens[_current - 1];
    private bool IsAtEnd() => _current >= Tokens.Count;

    public static ParseResult<NamespaceUnit> Parse(List<Token> tokens, List<IError> errs)
    {
        var p = new Parser(tokens, errs);
        return p.ParseNamespace();
    }

    private ParseResult<NamespaceUnit> ParseNamespace()
    {
        var namespaceToken = Consume(TokenType.Namespace);
        if (namespaceToken is null)
            return ParseResult<NamespaceUnit>.WrongConstruct();

        var identifier = ParseIdentifier();
        if (identifier.Failed)
            return ParseResult<NamespaceUnit>.Error();

        Consume(TokenType.Semicolon);
        var declarations = new List<IDeclaration>();
        while (!IsAtEnd())
        {
            var decl = ParseDeclaration();
            if (!decl.Success)
            {
                SkipToNext(
                    TokenType.Namespace, TokenType.SquareLeft,
                    TokenType.Public, TokenType.Private,
                    TokenType.Protected);
                continue;
            }

            declarations.Add(decl.Value!);
        }

        return ParseResult<NamespaceUnit>.Successful(new NamespaceUnit(
            namespaceToken,
            identifier.Value,
            declarations,
            namespaceToken.Start,
            Previous().End
        ));
    }

    private ParseResult<IDeclaration> ParseDeclaration()
    {
        if (Check(TokenType.Namespace)) return ParseNamespace().Cast<IDeclaration>();

        var attributes = ParseAttributes();
        var accessModifier = ParseAccessModifier();
        if (!accessModifier.Success) ParseResult<IDeclaration>.WrongConstruct();

        if (Check(TokenType.Type)) return ParseTypeDeclaration(attributes, accessModifier.Value!);
        // if (Check(TokenType.Class)) return ParseClassDeclaration(attributes, accessModifier);
        // if (Match(TokenType.Enum)) return ParseEnumDeclaration(attributes, accessModifier);
        // if (Match(TokenType.Union)) return ParseUnionDeclaration(attributes, accessModifier);
        // if (Match(TokenType.Interface)) return ParseInterfaceDeclaration(attributes, accessModifier);

        var next = ConsumeAny();
        if (next is null)
        {
            return ParseResult<IDeclaration>.Error();
        }

        Errs.Add(new UnexpectedTokenError(
            next,
            TokenType.Namespace,
            TokenType.Class,
            TokenType.Enum,
            TokenType.Union,
            TokenType.Interface,
            TokenType.Type));

        return ParseResult<IDeclaration>.Error();

        ParseResult<IDeclaration> ParseTypeDeclaration(List<Attribute> attributes, AccessModifier accessModifier)
        {
            var typeToken = ConsumeAny()!;
            var identToken = Consume(TokenType.Identifier);
            if (identToken is null) return ParseResult<IDeclaration>.Error();

            var eql = Consume(TokenType.Equals);
            if (eql is null) return ParseResult<IDeclaration>.Error();

            var typExpr = ParseTypeExpression();
            if (!typExpr.Success) return ParseResult<IDeclaration>.Error();

            var semiColon = Consume(TokenType.Semicolon);
            if (semiColon is null) return ParseResult<IDeclaration>.Error();

            return ParseResult<IDeclaration>.Successful(new TypeDeclaration(
                attributes,
                accessModifier,
                typeToken,
                identToken,
                eql,
                typExpr.Value!,
                semiColon,
                attributes.Count == 0 ? typeToken.Start : attributes[0].Start,
                semiColon.End
            ));
        }
    }

    public ParseResult<TypeExpr> ParseTypeExpression()
    {
        var ident = ParseIdentifier();
        if (ident.Failed)
            return ParseResult<TypeExpr>.WrongConstruct();

        var generic = ParseGeneric(false);
        var identifier = ident.Value!;

        return ParseResult<TypeExpr>.Successful(new TypeExpr(
            identifier,
            generic.Value,
            identifier.Start,
            generic.GetOr(x => x.End, identifier.End)
        ));
    }

    private ParseResult<IGeneric> ParseGeneric(bool asDecl)
    {
        if (!Check(TokenType.LessThan)) return ParseResult<IGeneric>.WrongConstruct();
        var angleLeft = ConsumeAny()!;

        if (Check(TokenType.SquareLeft))
        {
            var sqrLeft = ConsumeAny()!;
            if (asDecl)
            {
                var param = ParseDelimited(ParseParam);
                var sqrRight = Consume(TokenType.SquareRight);
                if (sqrRight is null) return ParseResult<IGeneric>.Error();

                var angleRight = Consume(TokenType.GreaterThan);
                if (angleRight is null) return ParseResult<IGeneric>.Error();

                return ParseResult<IGeneric>.Successful(new CompileTimeGeneric(
                    angleLeft,
                    angleRight,
                    sqrLeft,
                    sqrRight,
                    param.Value,
                    angleLeft.Start,
                    angleRight.End
                ));
            }
            else
            {
                var param = ParseDelimited(ParseExpression);
                var sqrRight = Consume(TokenType.SquareRight);
                if (sqrRight is null) return ParseResult<IGeneric>.Error();

                var angleRight = Consume(TokenType.GreaterThan);
                if (angleRight is null) return ParseResult<IGeneric>.Error();

                return ParseResult<IGeneric>.Successful(new CompileTimeGenericArgs(
                    angleLeft,
                    angleRight,
                    sqrLeft,
                    sqrRight,
                    param.Value,
                    angleLeft.Start,
                    angleRight.End
                ));
            }
        }

        if (asDecl)
        {
            var ts = ParseDelimited(ParseGenericT);
            var angleRight = Consume(TokenType.GreaterThan);
            if (angleRight is null) return ParseResult<IGeneric>.Error();

            return ParseResult<IGeneric>.Successful(new RuntimeGeneric(
                angleLeft,
                angleRight,
                ts.Value,
                angleLeft.Start,
                angleRight.End
            ));
        }
        else
        {
            var ts = ParseDelimited(ParseTypeExpression);
            var angleRight = Consume(TokenType.GreaterThan);
            if (angleRight is null) return ParseResult<IGeneric>.Error();

            return ParseResult<IGeneric>.Successful(new RuntimeGenericArgs(
                angleLeft,
                angleRight,
                ts.Value,
                angleLeft.Start,
                angleRight.End
            ));
        }
    }

    private ParseResult<GenericT> ParseGenericT()
    {
        var ident = Consume(TokenType.Identifier);
        if (ident is null) return ParseResult<GenericT>.WrongConstruct();
        return ParseResult<GenericT>.Successful(new GenericT(ident));
    }

    private ParseResult<Param> ParseParam()
    {
        var type = ParseTypeExpression();
        if (!type.Success) return ParseResult<Param>.WrongConstruct();

        var ident = Consume(TokenType.Identifier);
        if (ident is null) return ParseResult<Param>.Error();

        var typeExpr = type.Value!;
        return ParseResult<Param>.Successful(new Param(
            typeExpr,
            ident,
            typeExpr.Start,
            ident.End
        ));
    }

    private ParseResult<AccessModifier> ParseAccessModifier()
    {
        var token = Consume(TokenType.Public, TokenType.Private, TokenType.Protected);
        return token is null
            ? ParseResult<AccessModifier>.WrongConstruct()
            : ParseResult<AccessModifier>.Successful(new AccessModifier(token));
    }

    private List<Attribute> ParseAttributes()
    {
        var lst = new List<Attribute>();

        while (Check(TokenType.SquareLeft))
        {
            var attr = ParseAttribute();
            if (!attr.Success) continue;
            lst.Add(attr.Value!);
        }

        return lst;
    }

    private ParseResult<Attribute> ParseAttribute()
    {
        var sqrLeft = Consume(TokenType.SquareLeft);
        if (sqrLeft is null) return ParseResult<Attribute>.WrongConstruct();

        var bodies = ParseDelimited(ParseAttributeBody);

        var sqrRight = Consume(TokenType.SquareRight);
        if (sqrRight is null) return ParseResult<Attribute>.Error();

        return ParseResult<Attribute>.Successful(new Attribute(
            sqrLeft,
            sqrRight,
            bodies.Value,
            sqrLeft.Start,
            sqrRight.End
        ));
    }

    private ParseResult<Attribute.Body> ParseAttributeBody()
    {
        Token? at = null;

        if (Check(TokenType.At))
        {
            at = ConsumeAny();
        }

        var ident = ParseIdentifier();
        if (!ident.Success) return ParseResult<Attribute.Body>.WrongConstruct();
        var identifier = ident.Value!;

        if (!Check(TokenType.ParenLeft))
        {
            return ParseResult<Attribute.Body>.Successful(new Attribute.Body(
                at,
                identifier,
                null,
                null,
                null,
                at?.Start ?? identifier.Start,
                identifier.End
            ));
        }

        var parenL = ConsumeAny();
        var exprs = ParseDelimited(ParseExpression);
        var parenR = Consume(TokenType.ParenRight);
        if (parenR is null) return ParseResult<Attribute.Body>.Error();

        return ParseResult<Attribute.Body>.Successful(new Attribute.Body(
            at,
            identifier,
            parenL,
            parenR,
            exprs.Value,
            at?.Start ?? identifier.Start,
            parenR.End
        ));
    }

    public ParseResult<Identifier> ParseIdentifier()
    {
        var initial = ParseSimpleIdentifier();
        if (!initial.Success) return ParseResult<Identifier>.WrongConstruct();

        var identifier = initial.Value!;
        var start = identifier.Start;

        return ParseIdentifierLeft(identifier, start);
    }

    private ParseResult<Identifier> ParseIdentifierLeft(Identifier right, int start)
    {
        if (!Check(TokenType.Dot))
        {
            return ParseResult<Identifier>.Successful(right);
        }

        var dot = ConsumeAny();
        var nextRight = TryParse(ParseSimpleIdentifier);
        if (nextRight.IsDifferentConstruct)
        {
            _current--; // unconsume the dot
            return ParseResult<Identifier>.Successful(right);
        }

        var identifier = nextRight.Value!;
        var newNode = new Identifier(
            right,
            dot,
            identifier.Right,
            start,
            identifier.End
        );

        return ParseIdentifierLeft(newNode, start);
    }

    private ParseResult<Identifier> ParseSimpleIdentifier()
    {
        if (!Check(TokenType.Identifier))
        {
            return ParseResult<Identifier>.WrongConstruct();
        }

        var token = ConsumeAny()!;
        return ParseResult<Identifier>.Successful(new Identifier(
            null,
            null,
            token,
            token.Start,
            token.End
        ));
    }

    public ParseResult<IExpression> ParseExpression()
    {
        return ParseLogicalOr();
    }

    private ParseResult<IExpression> ParseLogicalOr()
    {
        var left = ParseLogicalAnd();
        if (left.Failed) return ParseResult<IExpression>.Inherit(left);

        while (Check(TokenType.Or))
        {
            var op = ConsumeAny()!;
            var right = ParseLogicalAnd();
            if (right.Failed) return ParseResult<IExpression>.Error();

            var leftExpr = left.Value!;
            var rightExpr = right.Value!;
            left = ParseResult<IExpression>.Successful(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private ParseResult<IExpression> ParseLogicalAnd()
    {
        var left = ParseEquality();
        if (left.Failed) return ParseResult<IExpression>.Inherit(left);

        while (Check(TokenType.And))
        {
            var op = ConsumeAny()!;
            var right = ParseEquality();
            if (right.Failed) return ParseResult<IExpression>.Error();

            var leftExpr = left.Value!;
            var rightExpr = right.Value!;
            left = ParseResult<IExpression>.Successful(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private ParseResult<IExpression> ParseEquality()
    {
        var left = ParseRelational();
        if (left.Failed) return ParseResult<IExpression>.Inherit(left);

        while (Check(TokenType.Equality) || Check(TokenType.NotEqual))
        {
            var op = ConsumeAny()!;
            var right = ParseRelational();
            if (right.Failed) return ParseResult<IExpression>.Error();

            var leftExpr = left.Value!;
            var rightExpr = right.Value!;
            left = ParseResult<IExpression>.Successful(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private ParseResult<IExpression> ParseRelational()
    {
        var left = ParseAdditive();
        if (left.Failed) return ParseResult<IExpression>.Inherit(left);

        while (Check(TokenType.GreaterThan) || Check(TokenType.LessThan) ||
               Check(TokenType.GreaterThanEquality) || Check(TokenType.LessThanEquality))
        {
            var op = ConsumeAny()!;
            var right = ParseAdditive();
            if (right.Failed) return ParseResult<IExpression>.Error();

            var leftExpr = left.Value!;
            var rightExpr = right.Value!;
            left = ParseResult<IExpression>.Successful(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private ParseResult<IExpression> ParseAdditive()
    {
        var left = ParseMultiplicative();
        if (left.Failed) return ParseResult<IExpression>.Inherit(left);

        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            var op = ConsumeAny()!;
            var right = ParseMultiplicative();
            if (right.Failed) return ParseResult<IExpression>.Error();

            var leftExpr = left.Value!;
            var rightExpr = right.Value!;
            left = ParseResult<IExpression>.Successful(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private ParseResult<IExpression> ParseMultiplicative()
    {
        var left = ParseUnary();
        if (left.Failed) return ParseResult<IExpression>.Inherit(left);

        while (Check(TokenType.Star) || Check(TokenType.Slash) || Check(TokenType.Percent))
        {
            var op = ConsumeAny()!;
            var right = ParseUnary();
            if (right.Failed) return ParseResult<IExpression>.Error();

            var leftExpr = left.Value!;
            var rightExpr = right.Value!;
            left = ParseResult<IExpression>.Successful(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private ParseResult<IExpression> ParseUnary()
    {
        if (!Check(TokenType.Bang) && !Check(TokenType.Minus) && !Check(TokenType.Plus)) return ParsePrimaryExpr();

        var op = ConsumeAny()!;
        var expr = ParseUnary();
        if (expr.Failed) return ParseResult<IExpression>.Error();

        var expression = expr.Value!;
        return ParseResult<IExpression>.Successful(new UnaryExpr(
            op,
            expression,
            op.Start,
            expression.End
        ));
    }

    private ParseResult<IExpression> ParsePrimaryExpr()
    {
        if (Check(TokenType.ParenLeft))
        {
            var left = ConsumeAny()!;
            var expr = ParseExpression();
            if (expr.Failed) return ParseResult<IExpression>.Error();
            var right = Consume(TokenType.ParenRight);
            if (right is null) return ParseResult<IExpression>.Error();

            var expression = expr.Value!;
            return ParseResult<IExpression>.Successful(new ParenExpr(
                left,
                expression,
                right,
                left.Start,
                right.End
            ));
        }

        if (Check(TokenType.CurlyLeft))
        {
            var left = ConsumeAny()!;
            var stmts = ParseStatements();
            var right = Consume(TokenType.CurlyRight);
            if (right is null) return ParseResult<IExpression>.Error();

            return ParseResult<IExpression>.Successful(new BlockExpr(
                left,
                stmts,
                right,
                left.Start,
                right.End
            ));
        }

        if (Check(TokenType.If))
        {
            return ParseIfExpr();
        }

        if (Check(TokenType.Match))
        {
            return ParseMatchExpr();
        }

        if (Check(TokenType.LiteralNumber))
        {
            var num = ConsumeAny()!;
            return ParseResult<IExpression>.Successful(new LiteralExpr(num));
        }

        if (Check(TokenType.LiteralString))
        {
            var str = ConsumeAny()!;
            return ParseResult<IExpression>.Successful(new LiteralExpr(str));
        }

        if (Check(TokenType.LiteralBool))
        {
            var boolean = ConsumeAny()!;
            return ParseResult<IExpression>.Successful(new LiteralExpr(boolean));
        }

        if (Check(TokenType.This))
        {
            var thisToken = ConsumeAny()!;
            return ParseResult<IExpression>.Successful(new ThisExpr(thisToken));
        }

        if (Check(TokenType.At))
        {
            return ParseMacroCall();
        }

        if (Check(TokenType.CodeBlock))
        {
            return ParseResult<IExpression>.Successful(new CodeBlockExpr(ConsumeAny()!));
        }

        var ident = ParseIdentifier();
        if (ident.Failed) return ParseResult<IExpression>.WrongConstruct();
        var identifier = ident.Value!;

        if (Check(TokenType.LessThan))
        {
            var cr = _current;
            var err = Errs.Count;
            
            var generic = ParseGeneric(false);

            if (Check(TokenType.ParenLeft))
            {
                if (generic.Failed)
                {
                    return ParseResult<IExpression>.Error();
                }
                
                var left = ConsumeAny()!;
                var args = ParseDelimited(ParseExpression);
                var right = Consume(TokenType.ParenRight);
                if (right is null) return ParseResult<IExpression>.Error();

                return ParseResult<IExpression>.Successful(new FunctionCallExpr(
                    identifier,
                    generic.Value,
                    left,
                    args.Value,
                    right,
                    identifier.Start,
                    right.End
                ));
            }

            // unparse the generic cuz it wasn't supposed to be parsed
            _current = cr;
            Errs.RemoveRange(err, Errs.Count - err);
        }
        else if (Check(TokenType.ParenLeft))
        {
            var left = ConsumeAny()!;
            var args = ParseDelimited(ParseExpression);
            var right = Consume(TokenType.ParenRight);
            if (right is null) return ParseResult<IExpression>.Error();

            return ParseResult<IExpression>.Successful(new FunctionCallExpr(
                identifier,
                null,
                left,
                args.Value,
                right,
                identifier.Start,
                right.End
            ));
        }

        if (Check(TokenType.Equals))
        {
            var eql = ConsumeAny()!;
            var right = ParseExpression();
            if (right.Failed) return ParseResult<IExpression>.Error();

            return ParseResult<IExpression>.Successful(new AssignmentExpr(
                identifier,
                eql,
                right.Value!,
                identifier.Start,
                right.Value!.End
            ));
        }

        return ParseResult<IExpression>.Successful(identifier);
    }

    private ParseResult<IExpression> ParseIfExpr()
    {
        var ifToken = ConsumeAny()!;
        var condition = ParseExpression();
        if (condition.Failed) return ParseResult<IExpression>.Error();

        var leftBrace = Consume(TokenType.CurlyLeft);
        if (leftBrace is null) return ParseResult<IExpression>.Error();

        var thenStmts = ParseStatements();

        var rightBrace = Consume(TokenType.CurlyRight);
        if (rightBrace is null) return ParseResult<IExpression>.Error();

        List<IfExpr.ElseIf> elseIfs = new();

        while (Check(TokenType.Else))
        {
            var elseToken = ConsumeAny()!;

            // Check if this is an "else if"
            if (Check(TokenType.If))
            {
                var elseIfIf = ConsumeAny()!;

                var elseIfCond = ParseExpression();
                if (elseIfCond.Failed) return ParseResult<IExpression>.Error();

                var elseIfLeft = Consume(TokenType.CurlyLeft);
                if (elseIfLeft is null) return ParseResult<IExpression>.Error();

                var elseIfStmts = ParseStatements();

                var elseIfRight = Consume(TokenType.CurlyRight);
                if (elseIfRight is null) return ParseResult<IExpression>.Error();

                elseIfs.Add(new IfExpr.ElseIf(
                    elseToken,
                    elseIfIf,
                    elseIfCond.Value!,
                    new BlockExpr(elseIfLeft, elseIfStmts, elseIfRight, elseIfLeft.Start, elseIfRight.End),
                    elseToken.Start,
                    elseIfRight.End
                ));
            }
            else
            {
                // This is a regular else block
                var elseLeft = Consume(TokenType.CurlyLeft);
                if (elseLeft is null) return ParseResult<IExpression>.Error();

                var elseStmts = ParseStatements();

                var elseRight = Consume(TokenType.CurlyRight);
                if (elseRight is null) return ParseResult<IExpression>.Error();

                var elseBlock = new IfExpr.Else(
                    elseToken,
                    new BlockExpr(elseLeft, elseStmts, elseRight, elseLeft.Start, elseRight.End),
                    elseToken.Start,
                    elseRight.End
                );

                var conditionExpr = condition.Value!;
                return ParseResult<IExpression>.Successful(new IfExpr(
                    ifToken,
                    conditionExpr,
                    new BlockExpr(leftBrace, thenStmts, rightBrace, leftBrace.Start, rightBrace.End),
                    elseIfs,
                    elseBlock,
                    ifToken.Start,
                    elseRight.End
                ));
            }
        }

        var conditionExpr2 = condition.Value!;
        return ParseResult<IExpression>.Successful(new IfExpr(
            ifToken,
            conditionExpr2,
            new BlockExpr(leftBrace, thenStmts, rightBrace, leftBrace.Start, rightBrace.End),
            elseIfs,
            null,
            ifToken.Start,
            rightBrace.End
        ));
    }

    private ParseResult<IExpression> ParseMatchExpr()
    {
        var matchToken = ConsumeAny()!;

        var matchee = ParseExpression();
        if (matchee.Failed) return ParseResult<IExpression>.Error();

        var leftBrace = Consume(TokenType.CurlyLeft);
        if (leftBrace is null) return ParseResult<IExpression>.Error();

        var cases = ParseDelimited(ParseMatchCase);
        if (cases.Failed) return ParseResult<IExpression>.Error();

        var rightBrace = Consume(TokenType.CurlyRight);
        if (rightBrace is null) return ParseResult<IExpression>.Error();

        return ParseResult<IExpression>.Successful(new MatchExpr(
            matchToken,
            matchee.Value!,
            leftBrace,
            cases.Value!,
            rightBrace,
            matchToken.Start,
            rightBrace.End
        ));
    }

    private ParseResult<MatchExpr.IMatchCase> ParseMatchCase()
    {
        // First try to parse as UnionCase
        var unionCase = TryParse(ParseUnionCase);
        if (unionCase.Success) return unionCase;
        if (!unionCase.IsDifferentConstruct) return ParseResult<MatchExpr.IMatchCase>.Error();

        // Then try as AnyCase
        var anyCase = TryParse(ParseAnyCase);
        if (anyCase.Success) return anyCase;
        if (!anyCase.IsDifferentConstruct) return ParseResult<MatchExpr.IMatchCase>.Error();

        return ParseExpressionCase();
    }

    private ParseResult<MatchExpr.IMatchCase> ParseUnionCase()
    {
        var ident = ParseIdentifier();
        if (ident.Failed) return ParseResult<MatchExpr.IMatchCase>.Inherit(ident);

        var leftParen = Consume(TokenType.ParenLeft);
        if (leftParen is null) return ParseResult<MatchExpr.IMatchCase>.WrongConstruct();

        var @params = ParseDelimited(ParseMatchParam);

        var rightParen = Consume(TokenType.ParenRight);
        if (rightParen is null) return ParseResult<MatchExpr.IMatchCase>.Error();

        var arrow = Consume(TokenType.Arrow);
        if (arrow is null) return ParseResult<MatchExpr.IMatchCase>.Error();

        var body = ParseBlock();
        if (body.Failed) return ParseResult<MatchExpr.IMatchCase>.Error();

        var identifier = ident.Value!;
        return ParseResult<MatchExpr.IMatchCase>.Successful(new MatchExpr.UnionCase(
            identifier,
            leftParen,
            @params.Value!,
            rightParen,
            arrow,
            body.Value!,
            identifier.Start,
            body.Value!.End
        ));
    }

    private ParseResult<MatchExpr.IMatchCase> ParseAnyCase()
    {
        if (!Check(TokenType.Underscore)) return ParseResult<MatchExpr.IMatchCase>.WrongConstruct();

        var underscore = ConsumeAny()!;

        var arrow = Consume(TokenType.Arrow);
        if (arrow is null) return ParseResult<MatchExpr.IMatchCase>.Error();

        var body = ParseBlock();
        if (body.Failed) return ParseResult<MatchExpr.IMatchCase>.Error();

        return ParseResult<MatchExpr.IMatchCase>.Successful(new MatchExpr.AnyCase(
            underscore,
            arrow,
            body.Value!,
            underscore.Start,
            body.Value!.End
        ));
    }

    private ParseResult<MatchExpr.IMatchCase> ParseExpressionCase()
    {
        var expr = ParseExpression();
        if (expr.Failed) return ParseResult<MatchExpr.IMatchCase>.Inherit(expr);

        var arrow = Consume(TokenType.Arrow);
        if (arrow is null) return ParseResult<MatchExpr.IMatchCase>.Error();

        var body = ParseBlock();
        if (body.Failed) return ParseResult<MatchExpr.IMatchCase>.Error();

        var expression = expr.Value!;
        return ParseResult<MatchExpr.IMatchCase>.Successful(new MatchExpr.ExpressionCase(
            expression,
            arrow,
            body.Value!,
            expression.Start,
            body.Value!.End
        ));
    }

    private ParseResult<IMatchParam> ParseMatchParam()
    {
        if (Check(TokenType.Underscore))
        {
            var underscore = ConsumeAny()!;
            return ParseResult<IMatchParam>.Successful(new UnderscoreMatchParam(
                underscore,
                underscore.Start,
                underscore.End
            ));
        }

        var expr = ParseExpression();
        if (expr.Failed) return ParseResult<IMatchParam>.Error();

        var expression = expr.Value!;
        return ParseResult<IMatchParam>.Successful(new ExpressionMatchParam(
            expression,
            expression.Start,
            expression.End
        ));
    }

    private ParseResult<BlockExpr> ParseBlock()
    {
        if (!Check(TokenType.CurlyLeft)) return ParseResult<BlockExpr>.WrongConstruct();

        var left = ConsumeAny()!;
        var stmts = ParseStatements();
        var right = Consume(TokenType.CurlyRight);
        if (right is null) return ParseResult<BlockExpr>.Error();

        return ParseResult<BlockExpr>.Successful(new BlockExpr(
            left,
            stmts,
            right,
            left.Start,
            right.End
        ));
    }

    private ParseResult<IExpression> ParseMacroCall()
    {
        var at = ConsumeAny()!;

        var ident = ParseIdentifier();
        if (ident.Failed) return ParseResult<IExpression>.Error();

        var leftParen = Consume(TokenType.ParenLeft);
        if (leftParen is null) return ParseResult<IExpression>.Error();

        var args = ParseDelimited(ParseExpression);

        var rightParen = Consume(TokenType.ParenRight);
        if (rightParen is null) return ParseResult<IExpression>.Error();

        var identifier = ident.Value!;
        return ParseResult<IExpression>.Successful(new MacroCallExpr(
            at,
            identifier,
            leftParen,
            args.Value,
            rightParen,
            at.Start,
            rightParen.End
        ));
    }

    public List<IStatement> ParseStatements()
    {
        if (Check(TokenType.CurlyRight))
        {
            return [];
        }

        var result = new List<IStatement>();
        var tri = TryParse(ParseStatement);

        while (tri.Success)
        {
            result.Add(tri.Value!);

            if (Check(TokenType.CurlyRight))
            {
                break;
            }

            tri = TryParse(ParseStatement);
        }

        return result;
    }

    private ParseResult<IStatement> ParseStatement()
    {
        // Handle return statement
        if (Check(TokenType.Return))
        {
            var returnToken = ConsumeAny()!;
            var expr1 = ParseExpression();
            if (expr1.Failed) return ParseResult<IStatement>.Error();

            var semicolon = Consume(TokenType.Semicolon);
            if (semicolon is null) return ParseResult<IStatement>.Error();

            var expression1 = expr1.Value!;
            return ParseResult<IStatement>.Successful(new ReturnStatement(
                returnToken,
                expression1,
                semicolon,
                returnToken.Start,
                semicolon.End
            ));
        }

        // Try to parse variable declaration first
        var varDecl = TryParse(ParseVarDecl);
        if (varDecl.Success) return varDecl;
        if (!varDecl.IsDifferentConstruct) return ParseResult<IStatement>.Error();

        // If not a variable declaration, must be an expression statement
        var expr = ParseExpression();
        if (expr.Failed) return ParseResult<IStatement>.Error();

        var semi = Consume(TokenType.Semicolon);
        if (semi is null) return ParseResult<IStatement>.Error();

        var expression = expr.Value!;
        return ParseResult<IStatement>.Successful(new ExpressionStatement(
            expression,
            semi,
            expression.Start,
            semi.End
        ));
    }

    private ParseResult<IStatement> ParseVarDecl()
    {
        var type = ParseTypeExpression();
        if (type.Failed) return ParseResult<IStatement>.WrongConstruct();

        var ident = Consume(TokenType.Identifier);
        if (ident is null) return ParseResult<IStatement>.WrongConstruct();

        Token? equals = null;
        IExpression? initializer = null;

        if (Check(TokenType.Equals))
        {
            equals = ConsumeAny();
            var init = ParseExpression();
            if (init.Failed) return ParseResult<IStatement>.Error();
            initializer = init.Value;
        }

        var semicolon = Consume(TokenType.Semicolon);
        if (semicolon is null) return ParseResult<IStatement>.Error();

        var typeExpr = type.Value!;
        return ParseResult<IStatement>.Successful(new VariableDeclaration(
            typeExpr,
            ident,
            equals,
            initializer,
            semicolon,
            typeExpr.Start,
            semicolon.End
        ));
    }

    private ParseResult<Delimited<T>> ParseDelimited<T>(Func<ParseResult<T>> parser)
        where T : IAstElement
    {
        var first = TryParse(parser);
        if (first.Failed)
        {
            return ParseResult<Delimited<T>>.Inherit(first);
        }

        var astElement = first.Value!;
        if (!Check(TokenType.Comma))
        {
            return ParseResult<Delimited<T>>.Successful(new Delimited<T>(
                astElement,
                null,
                null,
                astElement.Start,
                astElement.End
            ));
        }

        var comma = ConsumeAny()!;
        var next = TryParse(() => ParseNext(comma, parser));

        if (next.IsDifferentConstruct)
        {
            return ParseResult<Delimited<T>>.Successful(new Delimited<T>(
                astElement,
                null,
                comma,
                astElement.Start,
                astElement.End
            ));
        }

        if (next.Failed)
        {
            return ParseResult<Delimited<T>>.Error();
        }

        Token? trailingComma = null;
        var nextUnwrapped = next.Value!;

        if (Check(TokenType.Comma))
        {
            trailingComma = ConsumeAny();
        }

        return ParseResult<Delimited<T>>.Successful(new Delimited<T>(
            astElement,
            nextUnwrapped,
            trailingComma,
            astElement.Start,
            nextUnwrapped.End
        ));
    }

    private ParseResult<Delimited<T>.Next> ParseNext<T>(Token comma, Func<ParseResult<T>> parser)
        where T : IAstElement
    {
        var result = parser();

        if (result.Failed)
        {
            return ParseResult<Delimited<T>.Next>.Inherit(result);
        }

        var astElement = result.Value!;

        if (!Check(TokenType.Comma))
        {
            return ParseResult<Delimited<T>.Next>.Successful(new Delimited<T>.Next(
                comma,
                astElement,
                null,
                comma.Start,
                astElement.End
            ));
        }

        var commaNext = ConsumeAny()!;
        var next = TryParse(() => ParseNext(commaNext, parser));

        if (next.Failed)
        {
            if (!next.IsDifferentConstruct)
            {
                return ParseResult<Delimited<T>.Next>.Error();
            }

            // unconsume commaNext
            _current--;
        }

        return ParseResult<Delimited<T>.Next>.Successful(new Delimited<T>.Next(
            comma,
            astElement,
            next.Value,
            comma.Start,
            next.Failed ? astElement.End : next.Value!.End
        ));
    }

    private ParseResult<T> TryParse<T>(Func<ParseResult<T>> parser)
    {
        var curr = _current;
        var currErr = Errs.Count;

        var t = parser();
        if (t.Success)
        {
            return t;
        }

        _current = curr;
        if (!t.IsDifferentConstruct)
        {
            return ParseResult<T>.Error();
        }

        if (Errs.Count != currErr)
        {
            Errs.RemoveRange(currErr, Errs.Count - currErr);
        }

        return ParseResult<T>.WrongConstruct();
    }

    private void SkipToNext(params TokenType[] token)
    {
        while (!IsAtEnd() && !CheckAny(token))
        {
            Advance();
        }
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    private bool CheckAny(params TokenType[] types)
    {
        return types.Length == 0 || types.Any(Check);
    }

    /// <summary>
    /// Consumes the next token if it matches the type, if not an error reported but the next token is not consumed
    /// </summary>
    /// <param name="type">Any valid types to consume</param>
    /// <returns>The token consumed, null if token found does not match</returns>
    private Token? Consume(params TokenType[] type)
    {
        if (CheckAny(type)) return Advance();
        Errs.Add(new UnexpectedTokenError(Peek(), type));
        return null;
    }

    private Token? ConsumeAny()
    {
        if (!IsAtEnd()) return Advance();
        Errs.Add(new UnexpectedEofError());
        return null;
    }
}