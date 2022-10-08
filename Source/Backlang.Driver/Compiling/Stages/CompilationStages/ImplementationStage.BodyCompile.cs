﻿using Backlang.Codeanalysis.Core;
using Backlang.Contracts.Scoping.Items;
using Backlang.Driver.Core.Implementors;
using Backlang.Driver.Core.Implementors.Expressions;
using Backlang.Driver.Core.Implementors.Statements;
using Furesoft.Core.CodeDom.Compiler.Core.Collections;
using Furesoft.Core.CodeDom.Compiler.Instructions;

namespace Backlang.Driver.Compiling.Stages.CompilationStages;

public partial class ImplementationStage
{
    private static readonly ImmutableDictionary<Symbol, IStatementImplementor> _implementations = new Dictionary<Symbol, IStatementImplementor>()
    {
        [CodeSymbols.Var] = new VariableImplementor(),
        [CodeSymbols.If] = new IfImplementor(),
        [CodeSymbols.Assign] = new AssignmentImplementor(),
        [CodeSymbols.While] = new WhileImplementor(),
        [CodeSymbols.Return] = new ReturnImplementor(),
        [CodeSymbols.Throw] = new ThrowImplementor(),
        [CodeSymbols.ColonColon] = new StaticCallImplementor(),
        [CodeSymbols.Dot] = new CallImplementor(),
    }.ToImmutableDictionary();

    private static readonly ImmutableList<IExpressionImplementor> _expressions = new List<IExpressionImplementor>()
    {
        new TupleExpressionImplementor(),
        new ArrayExpressionImplementor(),
        new DefaultExpressionImplementor(),
        new AddressExpressionImplementor(),
        new UnaryExpressionImplementor(),
        new BinaryExpressionImplementor(),
        new IdentifierExpressionImplementor(),
        new PointerExpressionImplementor(),
        new ConstantExpressionEmitter(),
        new StaticCallImplementor(),

        new CallExpressionEmitter(), //should be added as last
    }.ToImmutableList();

    public static MethodBody CompileBody(LNode function, CompilerContext context, IMethod method,
                    QualifiedName? modulename, Scope scope)
    {
        var graph = Utils.CreateGraphBuilder();
        var block = graph.EntryPoint;

        AppendBlock(function.Args[3], block, context, method, modulename, scope);

        return new MethodBody(
            method.ReturnParameter,
            method.IsStatic ? new Parameter() : Parameter.CreateThisParameter(method.ParentType),
            EmptyArray<Parameter>.Value,
            graph.ToImmutable());
    }

    public static BasicBlockBuilder AppendBlock(LNode blkNode, BasicBlockBuilder block, CompilerContext context, IMethod method, QualifiedName? modulename, Scope scope)
    {
        foreach (var node in blkNode.Args)
        {
            if (!node.IsCall) continue;

            if (node.Calls(CodeSymbols.Braces))
            {
                if (node.ArgCount == 0) continue;

                block = AppendBlock(node, block.Graph.AddBasicBlock(), context, method, modulename, scope.CreateChildScope());
                continue;
            }

            if (_implementations.ContainsKey(node.Name))
            {
                block = _implementations[node.Name].Implement(context, method, block, node, modulename, scope);

                if (block == null)
                    return block;
            }
            else if (node.Calls("print"))
            {
                AppendCall(context, block, node, context.writeMethods, scope, modulename.Value, "Write");
            }
            else if (node.Calls("println"))
            {
                AppendCall(context, block, node, context.writeMethods, scope, modulename.Value, "WriteLine");
            }
            else
            {
                //ToDo: continue implementing static function call in same type
                var type = (DescribedType)method.ParentType;
                var calleeName = node.Target;
                var methods = type.Methods;

                if (!methods.Any(_ => _.Name.ToString() == calleeName.ToString()))
                {
                    type = (DescribedType)context.Binder.ResolveTypes(new SimpleName(Names.FreeFunctions).Qualify(modulename.Value)).FirstOrDefault();

                    if (type == null)
                    {
                        context.AddError(node, $"Cannot find function '{calleeName}'");
                    }
                }

                if (scope.TryGet<FunctionScopeItem>(calleeName.Name.Name, out var callee))
                {
                    if (type.IsStatic && !callee.IsStatic)
                    {
                        context.AddError(node, $"A non static function '{calleeName.Name.Name}' cannot be called in a static function.");
                        return block;
                    }

                    //ToDo: add overload AppendCall with known callee
                    AppendCall(context, block, node, type.Methods, scope, modulename.Value);
                }
                else
                {
                    var suggestion = LevensteinDistance.Suggest(calleeName.Name.Name, type.Methods.Select(_ => _.Name.ToString()));

                    context.AddError(node, $"Cannot find function '{calleeName.Name.Name}'. Did you mean '{suggestion}'?");
                }
            }
        }

        return block;
    }

    public static NamedInstructionBuilder AppendExpression(BasicBlockBuilder block, LNode node,
        IType elementType, CompilerContext context, Scope scope, QualifiedName? modulename)
    {
        var fetch = _expressions.FirstOrDefault(_ => _.CanHandle(node));

        return fetch == null ? null : fetch.Handle(node, block, elementType, context, scope, modulename);
    }

    public static NamedInstructionBuilder AppendCall(CompilerContext context, BasicBlockBuilder block,
        LNode node, IEnumerable<IMethod> methods, Scope scope, QualifiedName? modulename, string methodName = null)
    {
        var argTypes = new List<IType>();

        foreach (var arg in node.Args)
        {
            var type = TypeDeducer.Deduce(arg, scope, context, modulename.Value);

            if (type != null)
                argTypes.Add(type);
        }

        if (methodName == null)
        {
            methodName = node.Name.Name;
        }

        var method = GetMatchingMethod(context, argTypes, methods, methodName);

        return AppendCall(context, block, node, method, scope, modulename.Value);
    }

    public static NamedInstructionBuilder AppendCall(CompilerContext context, BasicBlockBuilder block,
        LNode node, IMethod method, Scope scope, QualifiedName? modulename)
    {
        var argTypes = new List<IType>();
        var callTags = new List<ValueTag>();

        foreach (var arg in node.Args)
        {
            var type = TypeDeducer.Deduce(arg, scope, context, modulename.Value);
            argTypes.Add(type);

            if (!arg.IsId)
            {
                var val = AppendExpression(block, arg, type, context, scope, modulename);

                callTags.Add(val);
            }
            else
            {
                if (scope.TryGet<ScopeItem>(arg.Name.Name, out var scopeItem))
                {
                    ValueTag vt = null;
                    if (scopeItem is VariableScopeItem vsi)
                    {
                        vt = block.AppendInstruction(Instruction.CreateLoadLocal(vsi.Parameter));
                    }
                    else if (scopeItem is ParameterScopeItem psi)
                    {
                        vt = block.AppendInstruction(Instruction.CreateLoadArg(psi.Parameter));
                    }
                    else if (scopeItem is FieldScopeItem fsi)
                    {
                        vt = block.AppendInstruction(Instruction.CreateLoadField(fsi.Field));
                    }

                    callTags.Add(vt);
                }
                else
                {
                    var suggestion = LevensteinDistance.Suggest(arg.Name.Name, scope.GetAllScopeNames());

                    context.AddError(arg, $"{arg.Name.Name} cannot be found. Did you mean '{suggestion}'?");
                }
            }
        }

        if (method == null) return null;

        if (!method.IsStatic)
        {
            callTags.Insert(0, block.AppendInstruction(Instruction.CreateLoadArg(new Parameter(method.ParentType))));
        }

        var call = Instruction.CreateCall(method, method.IsStatic ? MethodLookup.Static : MethodLookup.Virtual, callTags);

        return block.AppendInstruction(call);
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