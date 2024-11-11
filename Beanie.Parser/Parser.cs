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

    public static NamespaceUnit Parse(List<Token> tokens, List<IError> errs)
    {
        var p = new Parser(tokens, errs);
        if (p.Check(TokenType.Namespace))
        {
            return p.ParseNamespace();
        }

        var decls = new List<IDeclaration>();

        while (!p.IsAtEnd())
        {
            var decl = p.ParseDeclaration();
            if (decl.IsEmpty()) continue;
            decls.Add(decl.Unwrap()!);
        }

        return new NamespaceUnit(null, null, decls, 0, p.Previous().End);
    }

    private NamespaceUnit ParseNamespace()
    {
        var namespaceToken = ConsumeAny()!;
        var identifier = ParseIdentifier();
        if (identifier.IsEmpty())
            Consume(TokenType.Semicolon);

        var declarations = new List<IDeclaration>();
        while (!IsAtEnd())
        {
            var decl = ParseDeclaration();
            if (decl.IsEmpty()) continue;
            declarations.Add(decl.Unwrap()!);
        }

        return new NamespaceUnit(
            namespaceToken,
            identifier.Unwrap(),
            declarations,
            namespaceToken.Start,
            Previous().End
        );
    }

    public Opt<IDeclaration> ParseDeclaration()
    {
        if (Check(TokenType.Namespace)) return Opt<IDeclaration>.Some(ParseNamespace());

        var attributes = ParseAttributes();
        var accessModifier = ParseAccessModifier();
        if (accessModifier.IsEmpty()) return Opt<IDeclaration>.None();

        if (Check(TokenType.Type)) return ParseTypeDeclaration(attributes, accessModifier.Unwrap()!);
        // if (Check(TokenType.Class)) return ParseClassDeclaration(attributes, accessModifier);
        // if (Match(TokenType.Enum)) return ParseEnumDeclaration(attributes, accessModifier);
        // if (Match(TokenType.Union)) return ParseUnionDeclaration(attributes, accessModifier);
        // if (Match(TokenType.Interface)) return ParseInterfaceDeclaration(attributes, accessModifier);

        var next = ConsumeAny();

        if (next is null)
        {
            // error already handled
            return Opt<IDeclaration>.None();
        }

        Errs.Add(new UnexpectedTokenError(
            next,
            TokenType.Namespace,
            TokenType.Class,
            TokenType.Enum,
            TokenType.Union,
            TokenType.Interface,
            TokenType.Type));

        return Opt<IDeclaration>.None();
    }

    public Opt<IDeclaration> ParseTypeDeclaration(List<Attribute> attributes, AccessModifier accessModifier)
    {
        var typeToken = ConsumeAny()!;
        var identToken = Consume(TokenType.Identifier);
        if (identToken is null) return Opt<IDeclaration>.None();

        var eql = Consume(TokenType.Equals);
        if (eql is null) return Opt<IDeclaration>.None();

        var typExpr = ParseTypeExpression();
        if (typExpr.IsEmpty()) return Opt<IDeclaration>.None();

        var semiColon = Consume(TokenType.Semicolon);
        if (semiColon is null) return Opt<IDeclaration>.None();

        return Opt<IDeclaration>.Some(new TypeDeclaration(
            attributes,
            accessModifier,
            typeToken,
            identToken,
            eql,
            typExpr.Unwrap()!,
            semiColon,
            attributes.Count == 0 ? typeToken.Start : attributes[0].Start,
            semiColon.End
        ));
    }

    public Opt<TypeExpr> ParseTypeExpression()
    {
        var ident = ParseIdentifier();
        if (ident.IsEmpty()) return Opt<TypeExpr>.None();
        var generic = ParseGeneric(false);

        var identifier = ident.Unwrap()!;
        return Opt<TypeExpr>.Some(new TypeExpr(
            identifier,
            generic.Unwrap(),
            identifier.Start,
            generic.GetOr(x => x.End, identifier.End)
        ));
    }

    private Opt<IGeneric> ParseGeneric(bool asDecl)
    {
        if (!Check(TokenType.LessThan)) return Opt<IGeneric>.None();

        var angleLeft = ConsumeAny();
        if (angleLeft is null) return Opt<IGeneric>.None();

        if (Check(TokenType.SquareLeft))
        {
            var sqrLeft = ConsumeAny()!;
            if (asDecl)
            {
                var param = ParseDelimited(ParseParam);

                var sqrRight = Consume(TokenType.SquareRight);
                if (sqrRight is null) return Opt<IGeneric>.None();

                var angleRight = Consume(TokenType.GreaterThan);
                if (angleRight is null) return Opt<IGeneric>.None();

                return Opt<IGeneric>.Some(new CompileTimeGeneric(
                    angleLeft,
                    angleRight,
                    sqrLeft,
                    sqrRight,
                    param.Unwrap(),
                    angleLeft.Start,
                    angleRight.End
                ));
            }
            else
            {
                var param = ParseDelimited(ParseExpression);

                var sqrRight = Consume(TokenType.SquareRight);
                if (sqrRight is null) return Opt<IGeneric>.None();

                var angleRight = Consume(TokenType.GreaterThan);
                if (angleRight is null) return Opt<IGeneric>.None();

                return Opt<IGeneric>.Some(new CompileTimeGenericArgs(
                    angleLeft,
                    angleRight,
                    sqrLeft,
                    sqrRight,
                    param.Unwrap(),
                    angleLeft.Start,
                    angleRight.End
                ));
            }
        }

        if (asDecl)
        {
            var ts = ParseDelimited(ParseGenericT);
            var angleRight = Consume(TokenType.GreaterThan);
            if (angleRight is null) return Opt<IGeneric>.None();

            return Opt<IGeneric>.Some(new RuntimeGeneric(
                angleLeft,
                angleRight,
                ts.Unwrap(),
                angleLeft.Start,
                angleRight.End
            ));
        }
        else
        {
            var ts = ParseDelimited(ParseTypeExpression);
            var angleRight = Consume(TokenType.GreaterThan);
            if (angleRight is null) return Opt<IGeneric>.None();

            return Opt<IGeneric>.Some(new RuntimeGenericArgs(
                angleLeft,
                angleRight,
                ts.Unwrap(),
                angleLeft.Start,
                angleRight.End
            ));
        }
    }

    private Opt<GenericT> ParseGenericT()
    {
        var ident = Consume(TokenType.Identifier);
        if (ident is null) return Opt<GenericT>.None();
        return Opt<GenericT>.Some(new GenericT(ident));
    }

    public Opt<Param> ParseParam()
    {
        var type = ParseTypeExpression();
        if (type.IsEmpty()) return Opt<Param>.None();

        var ident = Consume(TokenType.Identifier);
        if (ident is null) return Opt<Param>.None();

        var typeExpr = type.Unwrap()!;
        return Opt<Param>.Some(new Param(
            typeExpr,
            ident,
            typeExpr.Start,
            ident.End
        ));
    }

    private Opt<AccessModifier> ParseAccessModifier()
    {
        var token = Consume(TokenType.Public, TokenType.Private, TokenType.Protected);
        return token is null ? Opt<AccessModifier>.None() : Opt<AccessModifier>.Some(new AccessModifier(token));
    }

    public List<Attribute> ParseAttributes()
    {
        var lst = new List<Attribute>();

        while (CheckAny(TokenType.SquareLeft))
        {
            var attr = ParseAttribute();
            if (attr.IsEmpty()) break;
            lst.Add(attr.Unwrap()!);
        }

        return lst;
    }

    private Opt<Attribute> ParseAttribute()
    {
        var sqrLeft = Consume(TokenType.SquareLeft);
        if (sqrLeft is null) return Opt<Attribute>.None();

        var bodies = ParseDelimited(ParseAttributeBody);

        var sqrRight = Consume(TokenType.SquareRight);
        if (sqrRight is null) return Opt<Attribute>.None();

        return Opt<Attribute>.Some(new Attribute(
            sqrLeft,
            sqrRight,
            bodies.Unwrap(),
            sqrLeft.Start,
            sqrRight.End
        ));
    }

    private Opt<Attribute.Body> ParseAttributeBody()
    {
        Token? at = null;

        if (Check(TokenType.At))
        {
            at = ConsumeAny();
        }

        var ident = ParseIdentifier();
        if (ident.IsEmpty()) return Opt<Attribute.Body>.None();
        var identifier = ident.Unwrap()!;

        if (!Check(TokenType.ParenLeft))
        {
            return Opt<Attribute.Body>.Some(new Attribute.Body(
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
        if (parenR is null) return Opt<Attribute.Body>.None();

        return Opt<Attribute.Body>.Some(new Attribute.Body(
            at,
            identifier,
            parenL,
            parenR,
            exprs.Unwrap(),
            at?.Start ?? identifier.Start,
            parenR.End
        ));
    }

    public Opt<Identifier> ParseIdentifier()
    {
        // Start with parsing the rightmost part
        var initial = ParseSimpleIdentifier();
        if (initial.IsEmpty()) return Opt<Identifier>.None();

        var identifier = initial.Unwrap()!;
        var start = identifier.Start;

        // Build up the chain from right to left
        return ParseIdentifierLeft(identifier, start);
    }

    private Opt<Identifier> ParseIdentifierLeft(Identifier right, int start)
    {
        // Look ahead for a dot
        if (!Check(TokenType.Dot))
        {
            return Opt<Identifier>.Some(right);
        }

        var dot = ConsumeAny();

        var nextRight = TryParse(ParseSimpleIdentifier);
        if (nextRight.IsEmpty())
        {
            _current--; // unconsume the dot
            return Opt<Identifier>.Some(right);
        }

        // Create new identifier node
        var identifier = nextRight.Unwrap()!;
        var newNode = new Identifier(
            right,
            dot,
            identifier.Right,
            start,
            identifier.End
        );

        // Recursively parse more left parts
        return ParseIdentifierLeft(newNode, start);
    }

    private Opt<Identifier> ParseSimpleIdentifier()
    {
        if (!Check(TokenType.Identifier))
        {
            return Opt<Identifier>.None();
        }

        var token = ConsumeAny()!;
        return Opt<Identifier>.Some(new Identifier(
            null,
            null,
            token,
            token.Start,
            token.End
        ));
    }

    public Opt<IExpression> ParseExpression()
    {
        return ParseLogicalOr();
    }

    private Opt<IExpression> ParseLogicalOr()
    {
        var left = ParseLogicalAnd();
        if (left.IsEmpty()) return Opt<IExpression>.None();

        while (Check(TokenType.Or))
        {
            var op = ConsumeAny()!;
            var right = ParseLogicalAnd();
            if (right.IsEmpty()) return Opt<IExpression>.None();

            var leftExpr = left.Unwrap()!;
            var rightExpr = right.Unwrap()!;
            left = Opt<IExpression>.Some(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private Opt<IExpression> ParseLogicalAnd()
    {
        var left = ParseEquality();
        if (left.IsEmpty()) return Opt<IExpression>.None();

        while (Check(TokenType.And))
        {
            var op = ConsumeAny()!;
            var right = ParseEquality();
            if (right.IsEmpty()) return Opt<IExpression>.None();

            var leftExpr = left.Unwrap()!;
            var rightExpr = right.Unwrap()!;
            left = Opt<IExpression>.Some(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private Opt<IExpression> ParseEquality()
    {
        var left = ParseRelational();
        if (left.IsEmpty()) return Opt<IExpression>.None();

        while (Check(TokenType.Equality) || Check(TokenType.NotEqual))
        {
            var op = ConsumeAny()!;
            var right = ParseRelational();
            if (right.IsEmpty()) return Opt<IExpression>.None();

            var leftExpr = left.Unwrap()!;
            var rightExpr = right.Unwrap()!;
            left = Opt<IExpression>.Some(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private Opt<IExpression> ParseRelational()
    {
        var left = ParseAdditive();
        if (left.IsEmpty()) return Opt<IExpression>.None();

        while (Check(TokenType.GreaterThan) || Check(TokenType.LessThan) ||
               Check(TokenType.GreaterThanEquality) || Check(TokenType.LessThanEquality))
        {
            var op = ConsumeAny()!;
            var right = ParseAdditive();
            if (right.IsEmpty()) return Opt<IExpression>.None();

            var leftExpr = left.Unwrap()!;
            var rightExpr = right.Unwrap()!;
            left = Opt<IExpression>.Some(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private Opt<IExpression> ParseAdditive()
    {
        var left = ParseMultiplicative();
        if (left.IsEmpty()) return Opt<IExpression>.None();

        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            var op = ConsumeAny()!;
            var right = ParseMultiplicative();
            if (right.IsEmpty()) return Opt<IExpression>.None();

            var leftExpr = left.Unwrap()!;
            var rightExpr = right.Unwrap()!;
            left = Opt<IExpression>.Some(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private Opt<IExpression> ParseMultiplicative()
    {
        var left = ParseUnary();
        if (left.IsEmpty()) return Opt<IExpression>.None();

        while (Check(TokenType.Star) || Check(TokenType.Slash) || Check(TokenType.Percent))
        {
            var op = ConsumeAny()!;
            var right = ParseUnary();
            if (right.IsEmpty()) return Opt<IExpression>.None();

            var leftExpr = left.Unwrap()!;
            var rightExpr = right.Unwrap()!;
            left = Opt<IExpression>.Some(new BinaryExpr(
                leftExpr,
                op,
                rightExpr,
                leftExpr.Start,
                rightExpr.End
            ));
        }

        return left;
    }

    private Opt<IExpression> ParseUnary()
    {
        if (Check(TokenType.Bang) || Check(TokenType.Minus) || Check(TokenType.Plus))
        {
            var op = ConsumeAny()!;
            var expr = ParseUnary();
            if (expr.IsEmpty()) return Opt<IExpression>.None();

            var expression = expr.Unwrap()!;
            return Opt<IExpression>.Some(new UnaryExpr(
                op,
                expression,
                op.Start,
                expression.End
            ));
        }

        return ParsePrimaryExpr();
    }

    private Opt<IExpression> ParsePrimaryExpr()
    {
        if (Check(TokenType.ParenLeft))
        {
            var left = ConsumeAny()!;
            var expr = ParseExpression();
            if (expr.IsEmpty()) return Opt<IExpression>.None();
            var right = Consume(TokenType.ParenRight);
            if (right is null) return Opt<IExpression>.None();

            var expression = expr.Unwrap()!;
            return Opt<IExpression>.Some(new ParenExpr(
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
            if (right is null) return Opt<IExpression>.None();

            return Opt<IExpression>.Some(new BlockExpr(
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
            return Opt<IExpression>.Some(new LiteralExpr(num));
        }

        if (Check(TokenType.LiteralString))
        {
            var str = ConsumeAny()!;
            return Opt<IExpression>.Some(new LiteralExpr(str));
        }

        if (Check(TokenType.LiteralBool))
        {
            var boolean = ConsumeAny()!;
            return Opt<IExpression>.Some(new LiteralExpr(boolean));
        }

        if (Check(TokenType.This))
        {
            var thisToken = ConsumeAny()!;
            return Opt<IExpression>.Some(new ThisExpr(thisToken));
        }

        if (Check(TokenType.At))
        {
            return ParseMacroCall();
        }

        if (Check(TokenType.CodeBlock))
        {
            return Opt<IExpression>.Some(new CodeBlockExpr(ConsumeAny()!));
        }

        // Try to parse as identifier (which might be a function call)
        var ident = ParseIdentifier();
        if (ident.IsEmpty()) return Opt<IExpression>.None();
        var identifier = ident.Unwrap()!;

        if (Check(TokenType.ParenLeft))
        {
            var left = ConsumeAny()!;
            var args = ParseDelimited(ParseExpression);
            var right = Consume(TokenType.ParenRight);
            if (right is null) return Opt<IExpression>.None();

            return Opt<IExpression>.Some(new FunctionCallExpr(
                identifier,
                left,
                args.Unwrap(),
                right,
                identifier.Start,
                right.End
            ));
        }

        if (Check(TokenType.Equals))
        {
            var eql = ConsumeAny()!;
            var right = ParseExpression();
            if (right.IsEmpty()) return Opt<IExpression>.None();

            return Opt<IExpression>.Some(new AssignmentExpr(
                identifier,
                eql,
                right.Unwrap()!,
                identifier.Start,
                right.Unwrap()!.End
            ));
        }

        return Opt<IExpression>.Some(identifier);
    }

    private Opt<IExpression> ParseIfExpr()
    {
        var ifToken = ConsumeAny()!;
        var condition = ParseExpression();
        if (condition.IsEmpty()) return Opt<IExpression>.None();

        var leftBrace = Consume(TokenType.CurlyLeft);
        if (leftBrace is null) return Opt<IExpression>.None();

        var thenStmts = ParseStatements();

        var rightBrace = Consume(TokenType.CurlyRight);
        if (rightBrace is null) return Opt<IExpression>.None();

        List<IfExpr.ElseIf> elseIfs = new();

        while (Check(TokenType.Else))
        {
            var elseToken = ConsumeAny()!;

            // Check if this is an "else if"
            if (Check(TokenType.If))
            {
                var elseIfIf = ConsumeAny()!;

                var elseIfCond = ParseExpression();
                if (elseIfCond.IsEmpty()) return Opt<IExpression>.None();

                var elseIfLeft = Consume(TokenType.CurlyLeft);
                if (elseIfLeft is null) return Opt<IExpression>.None();

                var elseIfStmts = ParseStatements();

                var elseIfRight = Consume(TokenType.CurlyRight);
                if (elseIfRight is null) return Opt<IExpression>.None();

                elseIfs.Add(new IfExpr.ElseIf(
                    elseToken,
                    elseIfIf,
                    elseIfCond.Unwrap()!,
                    new BlockExpr(elseIfLeft, elseIfStmts, elseIfRight, elseIfLeft.Start, elseIfRight.End),
                    elseToken.Start,
                    elseIfRight.End
                ));
            }
            else
            {
                // This is a regular else block
                var elseLeft = Consume(TokenType.CurlyLeft);
                if (elseLeft is null) return Opt<IExpression>.None();

                var elseStmts = ParseStatements();

                var elseRight = Consume(TokenType.CurlyRight);
                if (elseRight is null) return Opt<IExpression>.None();

                var elseBlock = new IfExpr.Else(
                    elseToken,
                    new BlockExpr(elseLeft, elseStmts, elseRight, elseLeft.Start, elseRight.End),
                    elseToken.Start,
                    elseRight.End
                );

                var conditionExpr = condition.Unwrap()!;
                return Opt<IExpression>.Some(new IfExpr(
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

        var conditionExpr2 = condition.Unwrap()!;
        return Opt<IExpression>.Some(new IfExpr(
            ifToken,
            conditionExpr2,
            new BlockExpr(leftBrace, thenStmts, rightBrace, leftBrace.Start, rightBrace.End),
            elseIfs,
            null,
            ifToken.Start,
            rightBrace.End
        ));
    }

    private Opt<IExpression> ParseMatchExpr()
    {
        var matchToken = ConsumeAny()!;

        var matchee = ParseExpression();
        if (matchee.IsEmpty()) return Opt<IExpression>.None();

        var leftBrace = Consume(TokenType.CurlyLeft);
        if (leftBrace is null) return Opt<IExpression>.None();

        var cases = ParseDelimited(ParseMatchCase);
        if (cases.IsEmpty()) return Opt<IExpression>.None();

        var rightBrace = Consume(TokenType.CurlyRight);
        if (rightBrace is null) return Opt<IExpression>.None();

        return Opt<IExpression>.Some(new MatchExpr(
            matchToken,
            matchee.Unwrap()!,
            leftBrace,
            cases.Unwrap()!,
            rightBrace,
            matchToken.Start,
            rightBrace.End
        ));
    }

    private Opt<MatchExpr.IMatchCase> ParseMatchCase()
    {
        // First try to parse as UnionCase
        var unionCase = TryParse(ParseUnionCase);
        if (unionCase.HasValue()) return unionCase;

        // Then try as AnyCase
        var anyCase = TryParse(ParseAnyCase);
        if (anyCase.HasValue()) return anyCase;

        // Finally try as ExpressionCase
        return ParseExpressionCase();
    }

    private Opt<MatchExpr.IMatchCase> ParseUnionCase()
    {
        var ident = ParseIdentifier();
        if (ident.IsEmpty()) return Opt<MatchExpr.IMatchCase>.None();

        var leftParen = Consume(TokenType.ParenLeft);
        if (leftParen is null) return Opt<MatchExpr.IMatchCase>.None();

        var @params = ParseDelimited(ParseMatchParam);

        var rightParen = Consume(TokenType.ParenRight);
        if (rightParen is null) return Opt<MatchExpr.IMatchCase>.None();

        var arrow = Consume(TokenType.Arrow);
        if (arrow is null) return Opt<MatchExpr.IMatchCase>.None();

        var body = ParseBlock();
        if (body.IsEmpty()) return Opt<MatchExpr.IMatchCase>.None();

        var identifier = ident.Unwrap()!;
        return Opt<MatchExpr.IMatchCase>.Some(new MatchExpr.UnionCase(
            identifier,
            leftParen,
            @params.Unwrap()!,
            rightParen,
            arrow,
            body.Unwrap()!,
            identifier.Start,
            body.Unwrap()!.End
        ));
    }

    private Opt<MatchExpr.IMatchCase> ParseAnyCase()
    {
        if (!Check(TokenType.Underscore)) return Opt<MatchExpr.IMatchCase>.None();

        var underscore = ConsumeAny()!;

        var arrow = Consume(TokenType.Arrow);
        if (arrow is null) return Opt<MatchExpr.IMatchCase>.None();

        var body = ParseBlock();
        if (body.IsEmpty()) return Opt<MatchExpr.IMatchCase>.None();

        return Opt<MatchExpr.IMatchCase>.Some(new MatchExpr.AnyCase(
            underscore,
            arrow,
            body.Unwrap()!,
            underscore.Start,
            body.Unwrap()!.End
        ));
    }

    private Opt<MatchExpr.IMatchCase> ParseExpressionCase()
    {
        var expr = ParseExpression();
        if (expr.IsEmpty()) return Opt<MatchExpr.IMatchCase>.None();

        var arrow = Consume(TokenType.Arrow);
        if (arrow is null) return Opt<MatchExpr.IMatchCase>.None();

        var body = ParseBlock();
        if (body.IsEmpty()) return Opt<MatchExpr.IMatchCase>.None();

        var expression = expr.Unwrap()!;
        return Opt<MatchExpr.IMatchCase>.Some(new MatchExpr.ExpressionCase(
            expression,
            arrow,
            body.Unwrap()!,
            expression.Start,
            body.Unwrap()!.End
        ));
    }

    private Opt<IMatchParam> ParseMatchParam()
    {
        if (Check(TokenType.Underscore))
        {
            var underscore = ConsumeAny()!;
            return Opt<IMatchParam>.Some(new UnderscoreMatchParam(
                underscore,
                underscore.Start,
                underscore.End
            ));
        }

        var expr = ParseExpression();
        if (expr.IsEmpty()) return Opt<IMatchParam>.None();

        var expression = expr.Unwrap()!;
        return Opt<IMatchParam>.Some(new ExpressionMatchParam(
            expression,
            expression.Start,
            expression.End
        ));
    }

    private Opt<BlockExpr> ParseBlock()
    {
        if (!Check(TokenType.CurlyLeft)) return Opt<BlockExpr>.None();

        var left = ConsumeAny()!;
        var stmts = ParseStatements();
        var right = Consume(TokenType.CurlyRight);
        if (right is null) return Opt<BlockExpr>.None();

        return Opt<BlockExpr>.Some(new BlockExpr(
            left,
            stmts,
            right,
            left.Start,
            right.End
        ));
    }

    private Opt<IExpression> ParseMacroCall()
    {
        var at = ConsumeAny()!;

        var ident = ParseIdentifier();
        if (ident.IsEmpty()) return Opt<IExpression>.None();

        var leftParen = Consume(TokenType.ParenLeft);
        if (leftParen is null) return Opt<IExpression>.None();

        var args = ParseDelimited(ParseExpression);

        var rightParen = Consume(TokenType.ParenRight);
        if (rightParen is null) return Opt<IExpression>.None();

        var identifier = ident.Unwrap()!;
        return Opt<IExpression>.Some(new MacroCallExpr(
            at,
            identifier,
            leftParen,
            args.Unwrap(),
            rightParen,
            at.Start,
            rightParen.End
        ));
    }

    public List<IStatement> ParseStatements()
    {
        var result = new List<IStatement>();

        var tri = TryParse(ParseStatement);
        while (tri.HasValue())
        {
            result.Add(tri.Unwrap()!);
            tri = TryParse(ParseStatement);
        }

        return result;
    }

    private Opt<IStatement> ParseStatement()
    {
        // Handle return statement
        if (Check(TokenType.Return))
        {
            var returnToken = ConsumeAny()!;
            var expr1 = ParseExpression();
            if (expr1.IsEmpty()) return Opt<IStatement>.None();

            var semicolon = Consume(TokenType.Semicolon);
            if (semicolon is null) return Opt<IStatement>.None();

            var expression1 = expr1.Unwrap()!;
            return Opt<IStatement>.Some(new ReturnStatement(
                returnToken,
                expression1,
                semicolon,
                returnToken.Start,
                semicolon.End
            ));
        }

        // Try to parse variable declaration first
        var varDecl = TryParse(ParseVarDecl);
        if (varDecl.HasValue()) return varDecl;

        // If not a variable declaration, must be an expression statement
        var expr = ParseExpression();
        if (expr.IsEmpty()) return Opt<IStatement>.None();

        var semi = Consume(TokenType.Semicolon);
        if (semi is null) return Opt<IStatement>.None();

        var expression = expr.Unwrap()!;
        return Opt<IStatement>.Some(new ExpressionStatement(
            expression,
            semi,
            expression.Start,
            semi.End
        ));
    }

    private Opt<IStatement> ParseVarDecl()
    {
        var type = ParseTypeExpression();
        if (type.IsEmpty()) return Opt<IStatement>.None();

        var ident = Consume(TokenType.Identifier);
        if (ident is null) return Opt<IStatement>.None();

        Token? equals = null;
        IExpression? initializer = null;

        if (Check(TokenType.Equals))
        {
            equals = ConsumeAny();
            var init = ParseExpression();
            if (init.IsEmpty()) return Opt<IStatement>.None();
            initializer = init.Unwrap();
        }

        var semicolon = Consume(TokenType.Semicolon);
        if (semicolon is null) return Opt<IStatement>.None();

        var typeExpr = type.Unwrap()!;
        return Opt<IStatement>.Some(new VariableDeclaration(
            typeExpr,
            ident,
            equals,
            initializer,
            semicolon,
            typeExpr.Start,
            semicolon.End
        ));
    }

    private Opt<Delimited<T>> ParseDelimited<T>(Func<Opt<T>> parser)
        where T : IAstElement
    {
        var first = TryParse(parser);
        if (!first.HasValue())
        {
            return Opt<Delimited<T>>.None();
        }

        var astElement = first.Unwrap()!;
        if (!Check(TokenType.Comma))
        {
            return Opt<Delimited<T>>.Some(new Delimited<T>(
                astElement,
                null,
                null,
                astElement.Start,
                astElement.End
            ));
        }

        var comma = ConsumeAny()!;
        var next = TryParse(() => ParseNext(comma, parser));

        if (next.IsEmpty())
        {
            return Opt<Delimited<T>>.Some(new Delimited<T>(
                astElement,
                null,
                comma,
                astElement.Start,
                astElement.End
            ));
        }

        Token? trailingComma = null;
        var nextUnwrapped = next.Unwrap()!;

        if (Check(TokenType.Comma))
        {
            trailingComma = ConsumeAny();
        }

        return Opt<Delimited<T>>.Some(new Delimited<T>(
            astElement,
            nextUnwrapped,
            trailingComma,
            astElement.Start,
            nextUnwrapped.End
        ));
    }

    private Opt<Delimited<T>.Next> ParseNext<T>(Token comma, Func<Opt<T>> parser)
        where T : IAstElement
    {
        var result = parser();

        if (result.IsEmpty())
        {
            return Opt<Delimited<T>.Next>.None();
        }

        var astElement = result.Unwrap()!;

        if (!Check(TokenType.Comma))
        {
            return Opt<Delimited<T>.Next>.Some(new Delimited<T>.Next(
                comma,
                astElement,
                null,
                comma.Start,
                astElement.End
            ));
        }

        var commaNext = ConsumeAny()!;
        var next = TryParse(() => ParseNext(commaNext, parser));

        if (next.IsEmpty())
        {
            // unconsume commaNext
            _current--;
        }

        return Opt<Delimited<T>.Next>.Some(new Delimited<T>.Next(
            comma,
            astElement,
            next.Unwrap(),
            comma.Start,
            next.IsEmpty() ? astElement.End : next.Unwrap()!.End
        ));
    }

    private Opt<T> TryParse<T>(Func<Opt<T>> parser, bool keepErr = false)
    {
        var curr = _current;
        var currErr = Errs.Count;

        var t = parser();
        if (t.HasValue())
        {
            return t;
        }

        _current = curr;
        if (!keepErr && Errs.Count != currErr)
        {
            Errs.RemoveRange(currErr, Errs.Count - currErr);
        }

        return Opt<T>.None();
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

    private Token? Consume(params TokenType[] type)
    {
        if (CheckAny(type)) return Advance();
        Errs.Add(new UnexpectedTokenError(Advance(), type));
        return null;
    }

    private Token? ConsumeAny()
    {
        if (!IsAtEnd()) return Advance();
        Errs.Add(new UnexpectedEofError());
        return null;
    }
}