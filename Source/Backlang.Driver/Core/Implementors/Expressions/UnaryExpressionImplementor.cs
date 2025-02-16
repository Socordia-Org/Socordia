﻿using Furesoft.Core.CodeDom.Compiler.Instructions;
using static Backlang.Driver.Compiling.Stages.CompilationStages.ImplementationStage;

namespace Backlang.Driver.Core.Implementors.Expressions;

public class UnaryExpressionImplementor : IExpressionImplementor
{
    public bool CanHandle(LNode node)
    {
        return node.ArgCount == 1 && !node.Calls(CodeSymbols.ColonColon) && node.Name.Name.StartsWith("'");
    }

    public NamedInstructionBuilder Handle(LNode node, BasicBlockBuilder block,
        IType elementType, CompilerContext context, Scope scope, QualifiedName? modulename)
    {
        var lhs = AppendExpression(block, node.Args[0], elementType, context, scope, modulename);

        var leftType = TypeDeducer.Deduce(node.Args[0], scope, context, modulename.Value);

        if (leftType.TryGetOperator(node.Name.Name, out var opMethod, leftType))
        {
            return block.AppendInstruction(
                Instruction.CreateCall(opMethod, MethodLookup.Static, new ValueTag[] { lhs }));
        }

        if (node.Calls(CodeSymbols._Dereference))
        {
            return block.AppendInstruction(Instruction.CreateLoadIndirect(leftType));
        }

        return block.AppendInstruction(Instruction.CreateArithmeticIntrinsic(node.Name.Name.Substring(1), false,
            elementType, new[] { leftType }, new ValueTag[] { lhs }));
    }
}