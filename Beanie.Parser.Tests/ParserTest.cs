using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parser.AST;
using Parser.AST.Expressions;
using Parser.AST.Generics;
using Parser.AST.Statements;
using Parser.Errors;

namespace Parser.Tests;

[TestClass]
[TestSubject(typeof(Parser))]
public class ParserTest
{
    [TestMethod]
    public void ParseTypeExpression()
    {
        //                                   0  12    3 4    56    78 91011
        var (tokens, errs) = Lexer.Tokenize("Res<Hello, Other.Class<It>, >");
        var p = new Parser(tokens, errs);
        var typeExpression = p.ParseTypeExpression();

        Assert.IsFalse(typeExpression.Failed);
        var expr = typeExpression.Value!;

        // Verify the main type identifier "Res"
        Assert.AreEqual(new Identifier(null, null, tokens[0], 0, tokens[0].End), expr.Identifier);

        // Verify the generic arguments
        Assert.IsNotNull(expr.Generic);
        var generic = (RuntimeGenericArgs)expr.Generic!;

        // Verify the generic delimiters
        Assert.AreEqual(tokens[1], generic.AngledL); // <
        Assert.AreEqual(tokens[11], generic.AngeldR); // >

        // Verify the generic arguments
        Assert.IsNotNull(generic.Generics);
        var args = generic.Generics!;

        // First argument: Hello
        var firstArg = args.First;
        Assert.AreEqual(new Identifier(null, null, tokens[2], tokens[2].Start, tokens[2].End), firstArg.Identifier);
        Assert.IsNull(firstArg.Generic);

        // Second argument: Other.Class<It>
        var secondArg = args.Second!.This;
        Assert.AreEqual(new Identifier(
            new Identifier(null, null, tokens[4], tokens[4].Start, tokens[4].End),
            tokens[5],
            tokens[6],
            tokens[4].Start,
            tokens[6].End
        ), secondArg.Identifier);

        // Verify nested generic
        var nestedGeneric = (RuntimeGenericArgs)secondArg.Generic!;
        Assert.AreEqual(tokens[7], nestedGeneric.AngledL);
        Assert.AreEqual(tokens[9], nestedGeneric.AngeldR);

        var nestedArg = nestedGeneric.Generics!.First;
        Assert.AreEqual(new Identifier(null, null, tokens[8], tokens[8].Start, tokens[8].End), nestedArg.Identifier);
    }

    [TestMethod]
    public void ParseIdentifier()
    {
        var (tokens, errs) = Lexer.Tokenize("System.API.This");
        var p = new Parser(tokens, errs);
        var ident = p.ParseIdentifier();

        Assert.IsFalse(ident.Failed);
        Assert.AreEqual(new Identifier(
            new Identifier(
                new Identifier(
                    null,
                    null,
                    tokens[0],
                    tokens[0].Start,
                    tokens[0].End
                ),
                tokens[1],
                tokens[2],
                tokens[0].Start,
                tokens[2].End
            ),
            tokens[3],
            tokens[4],
            tokens[0].Start,
            tokens[4].End
        ), ident.Value!);
    }

    [TestMethod]
    public void ParseBinaryExpression()
    {
        var (tokens, errs) = Lexer.Tokenize("1 + 2 * 3");
        var p = new Parser(tokens, errs);
        var expr = p.ParseExpression();

        Assert.IsFalse(expr.Failed);
        var binaryExpr = (BinaryExpr)expr.Value!;

        // Should parse as: (1 + (2 * 3)) due to operator precedence
        Assert.AreEqual(TokenType.Plus, binaryExpr.Operator.Type);

        Assert.IsInstanceOfType(binaryExpr.Left, typeof(LiteralExpr));
        Assert.AreEqual("1", ((LiteralExpr)binaryExpr.Left).LiteralToken.TokenData);

        Assert.IsInstanceOfType(binaryExpr.Right, typeof(BinaryExpr));
        var rightBinary = (BinaryExpr)binaryExpr.Right;
        Assert.AreEqual(TokenType.Star, rightBinary.Operator.Type);
        Assert.AreEqual("2", ((LiteralExpr)rightBinary.Left).LiteralToken.TokenData);
        Assert.AreEqual("3", ((LiteralExpr)rightBinary.Right).LiteralToken.TokenData);
    }

    [TestMethod]
    public void ParseFunctionCall()
    {
        var (tokens, errs) = Lexer.Tokenize("test.function(1, true, \"hello\", )");
        var p = new Parser(tokens, errs);
        var expr = p.ParseExpression();

        Assert.IsFalse(expr.Failed);
        var funcCall = (FunctionCallExpr)expr.Value!;

        // Verify function identifier
        Assert.AreEqual("function", funcCall.Function.Right.TokenData);
        Assert.AreEqual("test", funcCall.Function.Left!.Right.TokenData);

        // Verify arguments
        Assert.IsNotNull(funcCall.Arguments);
        var args = funcCall.Arguments!;

        // First argument
        Assert.IsInstanceOfType(args.First, typeof(LiteralExpr));
        Assert.AreEqual("1", ((LiteralExpr)args.First).LiteralToken.TokenData);

        // Second argument
        Assert.IsNotNull(args.Second);
        var second = args.Second!.This;
        Assert.IsInstanceOfType(second, typeof(LiteralExpr));
        Assert.AreEqual(true, ((LiteralExpr)second).LiteralToken.TokenData);

        // Third argument
        Assert.IsNotNull(args.Second.Other);
        var third = args.Second.Other!.This;
        Assert.IsInstanceOfType(third, typeof(LiteralExpr));
        Assert.AreEqual("hello", ((LiteralExpr)third).LiteralToken.TokenData);

        Assert.IsNotNull(args.Trailing);
    }

    [TestMethod]
    public void ParseIfExpression()
    {
        var (tokens, errs) = Lexer.Tokenize("if x > 5 { true; } else { false; }");
        var p = new Parser(tokens, errs);
        var expr = p.ParseExpression();

        Assert.IsFalse(expr.Failed);
        var ifExpr = (IfExpr)expr.Value!;

        // Verify condition
        Assert.IsInstanceOfType(ifExpr.Condition, typeof(BinaryExpr));
        var condition = (BinaryExpr)ifExpr.Condition;
        Assert.AreEqual(TokenType.GreaterThan, condition.Operator.Type);
        Assert.IsInstanceOfType(condition.Left, typeof(Identifier));
        Assert.IsInstanceOfType(condition.Right, typeof(LiteralExpr));

        // Verify then block
        Assert.IsNotNull(ifExpr.ThenBlock);
        Assert.AreEqual(1, ifExpr.ThenBlock.Statements.Count);

        // Verify else block
        Assert.IsNotNull(ifExpr.ElseClause);
        Assert.IsNotNull(ifExpr.ElseClause.ElseBlock);
        Assert.AreEqual(1, ifExpr.ElseClause.ElseBlock.Statements.Count);
    }

    [TestMethod]
    public void ParseMatchExpression()
    {
        var (tokens, errs) = Lexer.Tokenize(
            """
                    match value {
                        Some(x) => { x + 1; },
                        None() => { 0; },
                        Other(2, _, y) => { hello(); },
                        5 => { -1; },
                        _ => { @panic(); },
                    }
            """);

        var p = new Parser(tokens, errs);
        var expr = p.ParseExpression();

        Assert.IsFalse(expr.Failed);
        var matchExpr = (MatchExpr)expr.Value!;

        // Verify matchee
        Assert.IsInstanceOfType(matchExpr.Matchee, typeof(Identifier));
        Assert.AreEqual("value", ((Identifier)matchExpr.Matchee).Right.TokenData);

        // Verify cases
        Assert.IsNotNull(matchExpr.CaseList);
        var cases = matchExpr.CaseList;

        // First case (Some(x))
        Assert.IsInstanceOfType(cases.First, typeof(MatchExpr.UnionCase));
        var someCase = (MatchExpr.UnionCase)cases.First;
        Assert.AreEqual("Some", someCase.Case.Right.TokenData);
        Assert.IsNotNull(someCase.Params);
        Assert.IsInstanceOfType(someCase.Params.First, typeof(ExpressionMatchParam));
        Assert.IsInstanceOfType(((ExpressionMatchParam)someCase.Params.First).Expression, typeof(Identifier));

        // Second case (None())
        Assert.IsNotNull(cases.Second);
        Assert.IsInstanceOfType(cases.Second!.This, typeof(MatchExpr.UnionCase));
        var noneCase = (MatchExpr.UnionCase)cases.Second.This;
        Assert.AreEqual("None", noneCase.Case.Right.TokenData);

        // Third case (Other(2, _, y))
        Assert.IsNotNull(cases.Second.Other);
        var otherCase = (MatchExpr.UnionCase)cases.Second.Other!.This;
        Assert.AreEqual("Other", otherCase.Case.Right.TokenData);
        Assert.IsNotNull(otherCase.Params);

        // Verify parameters of Other case
        var otherParams = otherCase.Params;
        Assert.IsInstanceOfType(otherParams.First, typeof(ExpressionMatchParam));
        Assert.AreEqual("2",
            ((LiteralExpr)((ExpressionMatchParam)otherParams.First).Expression).LiteralToken.TokenData);

        Assert.IsNotNull(otherParams.Second);
        Assert.IsInstanceOfType(otherParams.Second!.This, typeof(UnderscoreMatchParam));

        Assert.IsNotNull(otherParams.Second.Other);
        Assert.IsInstanceOfType(otherParams.Second.Other!.This, typeof(ExpressionMatchParam));
        Assert.IsInstanceOfType(((ExpressionMatchParam)otherParams.Second.Other!.This).Expression, typeof(Identifier));

        // Fourth case (5 => ...)
        Assert.IsNotNull(cases.Second.Other.Other);
        Assert.IsInstanceOfType(cases.Second.Other.Other!.This, typeof(MatchExpr.ExpressionCase));
        var literalCase = (MatchExpr.ExpressionCase)cases.Second.Other.Other!.This;
        Assert.IsInstanceOfType(literalCase.Expression, typeof(LiteralExpr));
        Assert.AreEqual("5", ((LiteralExpr)literalCase.Expression).LiteralToken.TokenData);

        // Last case (_)
        Assert.IsNotNull(cases.Second.Other.Other.Other);
        Assert.IsInstanceOfType(cases.Second.Other.Other.Other!.This, typeof(MatchExpr.AnyCase));
    }

    [TestMethod]
    public void ParseComplexExpression()
    {
        var (tokens, errs) = Lexer.Tokenize(@"
        if x > 5 && y < 10 {
            match z {
                Some(v) => { v + 1; },
                None() => { 0; }
            };
        } else {
            x * (y + 2);
        }");
        var p = new Parser(tokens, errs);
        var expr = p.ParseExpression();

        Assert.IsFalse(expr.Failed);
        Assert.AreEqual(0, p.Errs.Count);

        var ifExpr = (IfExpr)expr.Value!;

        // Verify condition (x > 5 && y < 10)
        Assert.IsInstanceOfType(ifExpr.Condition, typeof(BinaryExpr));
        var condition = (BinaryExpr)ifExpr.Condition;
        Assert.AreEqual(TokenType.And, condition.Operator.Type);

        // Verify then block contains match expression
        Assert.IsNotNull(ifExpr.ThenBlock);
        Assert.AreEqual(1, ifExpr.ThenBlock.Statements.Count);
        Assert.IsInstanceOfType(((ExpressionStatement)ifExpr.ThenBlock.Statements[0]).Expression, typeof(MatchExpr));

        // Verify else block contains binary expression
        Assert.IsNotNull(ifExpr.ElseClause);
        Assert.IsNotNull(ifExpr.ElseClause.ElseBlock);
        Assert.AreEqual(1, ifExpr.ElseClause.ElseBlock.Statements.Count);
        Assert.IsInstanceOfType(((ExpressionStatement)ifExpr.ElseClause.ElseBlock.Statements[0]).Expression,
            typeof(BinaryExpr));
    }

    [TestMethod]
    public void ParseStatements()
    {
        var (tokens, errs) = Lexer.Tokenize(@"
            I32 x = 42;
            return x + 1;
            someFunction(1, 2);
            if true { 1; } else { 2; }; // todo
            x = 10;");

        var p = new Parser(tokens, errs);
        var statements = p.ParseStatements();

        Assert.AreEqual(5, statements.Count);
        Assert.AreEqual(0, errs.Count);

        // Variable declaration
        Assert.IsInstanceOfType(statements[0], typeof(VariableDeclaration));
        var varDecl = (VariableDeclaration)statements[0];
        Assert.AreEqual("I32", varDecl.Type.Identifier.Right.TokenData);
        Assert.AreEqual("x", varDecl.Identifier.TokenData);
        Assert.IsInstanceOfType(varDecl.InitialExpression, typeof(LiteralExpr));

        // Return statement
        Assert.IsInstanceOfType(statements[1], typeof(ReturnStatement));
        var returnStmt = (ReturnStatement)statements[1];
        Assert.IsInstanceOfType(returnStmt.Expression, typeof(BinaryExpr));

        // Expression statement (function call)
        Assert.IsInstanceOfType(statements[2], typeof(ExpressionStatement));
        var funcCall = (ExpressionStatement)statements[2];
        Assert.IsInstanceOfType(funcCall.Expression, typeof(FunctionCallExpr));

        // Expression statement (if expression)
        Assert.IsInstanceOfType(statements[3], typeof(ExpressionStatement));
        var ifExpr = (ExpressionStatement)statements[3];
        Assert.IsInstanceOfType(ifExpr.Expression, typeof(IfExpr));

        // Expression statement (assignment)
        Assert.IsInstanceOfType(statements[4], typeof(ExpressionStatement));
        var assignment = (ExpressionStatement)statements[4];
        Assert.IsInstanceOfType(assignment.Expression, typeof(AssignmentExpr));
    }

    [TestMethod]
    public void ParseMatchExpressionWithStatements()
    {
        var (tokens, errs) = Lexer.Tokenize(@"
            match value {
                Some(x) => {
                    I32 temp = x + 1;
                    return temp;
                },
                None() => {
                    Console.WriteLine(""Nothing"");
                    return 0;
                },
                Other(n, _, y) => {
                    I32 result = n + y;
                    if result > 10 {
                        return result;
                    } else {
                        return -1;
                    };
                }
            }");

        var p = new Parser(tokens, errs);
        var expr = p.ParseExpression();
        Assert.IsFalse(expr.Failed);
        Assert.AreEqual(0, errs.Count);
        var matchExpr = (MatchExpr)expr.Value!;
        // Check Some case statements
        var someCase = (MatchExpr.UnionCase)matchExpr.CaseList.First;
        Assert.AreEqual(2, someCase.Body.Statements.Count);
        Assert.IsInstanceOfType(someCase.Body.Statements[0], typeof(VariableDeclaration));
        Assert.IsInstanceOfType(someCase.Body.Statements[1], typeof(ReturnStatement));

        // Check None case statements
        var noneCase = (MatchExpr.UnionCase)matchExpr.CaseList.Second!.This;
        Assert.AreEqual(2, noneCase.Body.Statements.Count);
        Assert.IsInstanceOfType(noneCase.Body.Statements[0], typeof(ExpressionStatement));
        Assert.IsInstanceOfType(noneCase.Body.Statements[1], typeof(ReturnStatement));

        // Check Other case statements
        var otherCase = (MatchExpr.UnionCase)matchExpr.CaseList.Second.Other!.This;
        Assert.AreEqual(2, otherCase.Body.Statements.Count);
        Assert.IsInstanceOfType(otherCase.Body.Statements[0], typeof(VariableDeclaration));
        Assert.IsInstanceOfType(otherCase.Body.Statements[1], typeof(ExpressionStatement));

        // Verify the if-else in Other case
        var ifExpr = ((ExpressionStatement)otherCase.Body.Statements[1]).Expression as IfExpr;
        Assert.IsNotNull(ifExpr);
        Assert.IsNotNull(ifExpr!.ElseClause);
    }

    [TestMethod]
    public void ParseArithmetic()
    {
        var (tokens, errs) = Lexer.Tokenize("2 + x * (y - 1);");
        var p = new Parser(tokens, errs);
        var statements = p.ParseStatements();

        Assert.IsTrue(statements.Count == 1);
        Assert.IsInstanceOfType<ExpressionStatement>(statements[0]);

        var binExpr = (BinaryExpr)((ExpressionStatement)statements[0]).Expression;
        Assert.AreEqual(tokens[1], binExpr.Operator);
        Assert.AreEqual(new LiteralExpr(tokens[0]), binExpr.Left);
        Assert.IsInstanceOfType<BinaryExpr>(binExpr.Right);

        var right = (BinaryExpr)binExpr.Right;
        Assert.IsInstanceOfType<Identifier>(right.Left);
        Assert.AreEqual(right.Left, new Identifier(null, null, tokens[2], tokens[2].Start, tokens[2].End));

        Assert.IsInstanceOfType<ParenExpr>(right.Right);
        Assert.IsInstanceOfType<BinaryExpr>(((ParenExpr)right.Right).Inner);

        var inner = (BinaryExpr)((ParenExpr)right.Right).Inner;
        Assert.IsInstanceOfType<Identifier>(inner.Left);
        Assert.IsInstanceOfType<LiteralExpr>(inner.Right);
        Assert.AreEqual(new Identifier(null, null, tokens[5], tokens[5].Start, tokens[5].End), inner.Left);
        Assert.AreEqual(new LiteralExpr(tokens[7]), inner.Right);
    }

    [TestMethod]
    public void ParseStatement_WithErr()
    {
        var (tokens, errs) = Lexer.Tokenize("2 4;");
        var p = new Parser(tokens, errs);
        var stmts = p.ParseStatements();

        Assert.AreEqual(0, stmts.Count);
        Assert.AreEqual(1, errs.Count);
        Assert.IsInstanceOfType<UnexpectedTokenError>(errs[0]);
    }
}