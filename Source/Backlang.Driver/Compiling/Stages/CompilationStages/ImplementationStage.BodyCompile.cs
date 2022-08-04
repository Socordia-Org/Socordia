﻿using Backlang.Codeanalysis.Parsing.AST;
using Backlang.Contracts;
using Backlang.Contracts.Scoping;
using Backlang.Driver.Core.Implementors;
using Backlang.Driver.Core.Implementors.Expressions;
using Backlang.Driver.Core.Implementors.Statements;
using Furesoft.Core.CodeDom.Compiler;
using Furesoft.Core.CodeDom.Compiler.Core;
using Furesoft.Core.CodeDom.Compiler.Core.Collections;
using Furesoft.Core.CodeDom.Compiler.Core.Names;
using Furesoft.Core.CodeDom.Compiler.Core.TypeSystem;
using Furesoft.Core.CodeDom.Compiler.Instructions;
using Loyc;
using Loyc.Syntax;
using System.Collections.Immutable;

namespace Backlang.Driver.Compiling.Stages.CompilationStages;

public partial class ImplementationStage
{
    private static readonly ImmutableDictionary<Symbol, IStatementImplementor> _implementations = new Dictionary<Symbol, IStatementImplementor>()
    {
        [CodeSymbols.Var] = new VariableImplementor(),
        [CodeSymbols.If] = new IfImplementor(),
        [CodeSymbols.While] = new WhileImplementor(),
        [CodeSymbols.Return] = new ReturnImplementor(),
        [CodeSymbols.Throw] = new ThrowImplementor(),
        [CodeSymbols.ColonColon] = new StaticCallImplementor(),
        [CodeSymbols.Dot] = new CallImplementor(),
    }.ToImmutableDictionary();

    private static readonly ImmutableList<IExpressionImplementor> _expressions = new List<IExpressionImplementor>()
    {
        new AddressExpressionImplementor(),
        new BinaryExpressionImplementor(),
        new IdentifierExpressionImplementor(),
        new PointerExpressionImplementor(),
        new UnaryExpressionImplementor()
    }.ToImmutableList();

    public static MethodBody CompileBody(LNode function, CompilerContext context, IMethod method,
                    QualifiedName? modulename, Scope scope)
    {
        var graph = Utils.CreateGraphBuilder();
        var block = graph.EntryPoint;

        AppendBlock(function.Args[3], block, context, method, modulename, scope);

        return new MethodBody(
            method.ReturnParameter,
            new Parameter(method.ParentType),
            EmptyArray<Parameter>.Value,
            graph.ToImmutable());
    }

    public static BasicBlockBuilder AppendBlock(LNode blkNode, BasicBlockBuilder block, CompilerContext context, IMethod method, QualifiedName? modulename, Scope scope)
    {
        foreach (var node in blkNode.Args)
        {
            if (!node.IsCall) continue;

            if (node.Calls(Symbols.Block))
            {
                if (node.ArgCount == 0) continue;

                block = AppendBlock(node, block.Graph.AddBasicBlock(), context, method, modulename, scope.CreateChildScope());
            }

            if (_implementations.ContainsKey(node.Name))
            {
                block = _implementations[node.Name].Implement(context, method, block, node, modulename, scope);
            }
            else if (node.Calls("print"))
            {
                AppendCall(context, block, node, context.writeMethods, "Write");
            }
            else if (node.Calls("println"))
            {
                AppendCall(context, block, node, context.writeMethods, "WriteLine");
            }
            else
            {
                //ToDo: continue implementing static function call in same type
                var type = method.ParentType;
                var calleeName = node.Target;
                var callee = type.Methods.FirstOrDefault(_ => _.Name.ToString() == calleeName.Name.Name);

                if (callee != null)
                {
                    AppendCall(context, block, node, type.Methods);
                }
                else
                {
                    context.AddError(node, $"Cannot find function '{calleeName.Name.Name}'");
                }
            }
        }

        return block;
    }

    public static NamedInstructionBuilder AppendExpression(BasicBlockBuilder block, LNode node,
        IType elementType, IMethod method, CompilerContext context)
    {
        if (node.Calls(CodeSymbols.ColonColon))
        {
            var callee = node.Args[1];
            var typename = ConversionUtils.GetQualifiedName(node.Args[0]);

            var type = (DescribedType)context.Binder.ResolveTypes(typename).FirstOrDefault();

            AppendCall(context, block, callee, type.Methods, callee.Name.Name);
        }

        var fetch = _expressions.FirstOrDefault(_ => _.CanHandle(node));

        return fetch == null ? null : fetch.Handle(node, block, elementType, method, context);
    }

    public static void AppendCall(CompilerContext context, BasicBlockBuilder block,
        LNode node, IEnumerable<IMethod> methods, string methodName = null)
    {
        var argTypes = new List<IType>();
        var callTags = new List<ValueTag>();

        foreach (var arg in node.Args)
        {
            var type = GetLiteralType(arg, context.Binder);
            argTypes.Add(type);

            var constant = block.AppendInstruction(
            ConvertConstant(type, arg.Args[0].Value));

            block.AppendInstruction(Instruction.CreateLoad(type, constant));

            callTags.Add(constant);
        }

        if (methodName == null)
        {
            methodName = node.Name.Name;
        }

        var method = GetMatchingMethod(context, argTypes, methods, methodName);

        if (method == null) return;

        if (!method.IsStatic)
        {
            callTags.Insert(0, block.AppendInstruction(Instruction.CreateLoadArg(new Parameter(method.ParentType))));
        }

        var call = Instruction.CreateCall(method, method.IsStatic ? MethodLookup.Static : MethodLookup.Virtual, callTags);

        block.AppendInstruction(call);
    }

    private static void ConvertMethodBodies(CompilerContext context)
    {
        foreach (var bodyCompilation in context.BodyCompilations)
        {
            bodyCompilation.Method.Body =
                CompileBody(bodyCompilation.Function, context,
                bodyCompilation.Method, bodyCompilation.Modulename, bodyCompilation.Scope);
        }
    }
}