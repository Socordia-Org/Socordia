﻿using Furesoft.Core.CodeDom.Compiler.Instructions;
using static Backlang.Driver.Compiling.Stages.CompilationStages.ImplementationStage;

namespace Backlang.Driver.Core.Implementors.Expressions;

public class BinaryExpressionImplementor : IExpressionImplementor
{
    public bool CanHandle(LNode node)
    {
        return node.ArgCount == 2
               && !node.Calls(CodeSymbols.ColonColon)
               && !node.Calls(CodeSymbols.Tuple)
               && node.Name.Name.StartsWith("'");
    }

    public NamedInstructionBuilder Handle(LNode node, BasicBlockBuilder block,
        IType elementType, CompilerContext context, Scope scope, QualifiedName? modulename)
    {
        var lhs = AppendExpression(block, node.Args[0], elementType, context, scope, modulename);
        var rhs = AppendExpression(block, node.Args[1], elementType, context, scope, modulename);

        var leftType = TypeDeducer.Deduce(node.Args[0], scope, context, modulename.Value);
        var rightType = TypeDeducer.Deduce(node.Args[1], scope, context, modulename.Value);

        if (leftType.TryGetOperator(node.Name.Name, out var opMethod, leftType, rightType))
        {
            return block.AppendInstruction(Instruction.CreateCall(opMethod, MethodLookup.Static,
                new ValueTag[] { lhs, rhs }));
        }

        if (node.Calls(CodeSymbols.Add) &&
            (leftType == context.Environment.String || rightType == context.Environment.String))
        {
            var concatMethods = context.Environment.String.Methods
                .Where(_ => _.Name.ToString() == "Concat" && _.Parameters.Count == 2);

            var matchingConcatMethod = concatMethods.FirstOrDefault(_ =>
                _.Parameters[0].Type == leftType && _.Parameters[1].Type == rightType);

            var call = Instruction.CreateCall(matchingConcatMethod, MethodLookup.Static, new ValueTag[] { lhs, rhs });

            return block.AppendInstruction(call);
        }
        //ToDo: add check for unittype and no unitype. then unpack unit type

        return block.AppendInstruction(
            Instruction.CreateBinaryArithmeticIntrinsic(node.Name.Name.Substring(1), false, elementType, lhs, rhs));
    }
}