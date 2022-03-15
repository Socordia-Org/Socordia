﻿using Backlang.Codeanalysis.Parsing.AST.Declarations;
using Backlang.Codeanalysis.Parsing.AST.Expressions;
using Backlang.Codeanalysis.Parsing.AST.Expressions.Match;
using Backlang.Codeanalysis.Parsing.AST.Statements;

namespace Backlang.Codeanalysis.Parsing.AST;

public interface IVisitor<T>
{
    T Visit(InvalidNode invalidNode);

    T Visit(VariableDeclarationStatement variableDeclarationStatement);

    T Visit(LiteralNode literal);

    T Visit(StructDeclaration structDeclaration);

    T Visit(RegisterDeclaration registerDeclaration);

    T Visit(BitFieldDeclaration bitFieldDeclaration);

    T Visit(ExpressionStatement expressionStatement);

    T Visit(CompilationUnit compilationUnit);

    T Visit(BitFieldMemberDeclaration bitFieldMemberDeclaration);

    T Visit(EnumDeclaration enumDeclaration);

    T Visit(AssignmentStatement assignmentStatement);

    T Visit(BinaryExpression binaryExpression);

    T Visit(StructMemberDeclaration structMemberDeclaration);

    T Visit(UnaryExpression unaryExpression);

    T Visit(FunctionDeclaration functionDeclaration);

    T Visit(EnumMemberDeclaration enumMemberDeclaration);

    T Visit(GroupExpression groupExpression);

    T Visit(Block block);

    T Visit(InvalidExpr invalidExpr);

    T Visit(NameExpression nameExpression);

    T Visit(CallExpr callExpr);

    T Visit(MatchExpression expression);

    T Visit(DefaultExpression expression);

    T Visit(Expression expression);

    T Visit(ParameterDeclaration parameterDeclaration);

    T Visit(TypeLiteral typeLiteral);
}