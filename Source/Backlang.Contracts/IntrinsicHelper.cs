﻿using Furesoft.Core.CodeDom.Compiler;
using Furesoft.Core.CodeDom.Compiler.Core.Constants;
using Furesoft.Core.CodeDom.Compiler.Instructions;
using System.Reflection;

namespace Backlang.Contracts;

public static class IntrinsicHelper
{
    public static bool IsIntrinsicType(Type intrinsicType, CallPrototype callPrototype)
    {
        return callPrototype.Callee.ParentType.FullName.ToString() == intrinsicType.ToString();
    }

    public static object InvokeIntrinsic(Type intrinsicType, IMethod callee, Instruction instruction, BasicBlock block)
    {
        var method = GetMatchingIntrinsicMethod(callee, intrinsicType);

        var arguments = new List<object>();

        foreach (var argTag in instruction.Arguments)
        {
            var loadInstruction = block.Graph.GetInstruction(argTag);

            if (loadInstruction.Prototype is LoadPrototype)
            {
                var argPrototype =
                    (ConstantPrototype)block.Graph.GetInstruction(loadInstruction.Arguments[0]).Prototype;
                arguments.Add(GetValue(argPrototype.Value));
            }
        }

        return method.Invoke(null, arguments.ToArray());
    }

    private static object GetValue(Constant value)
    {
        switch (value)
        {
            case StringConstant str:
                return str.Value;

            case Float32Constant f32:
                return f32.Value;

            case Float64Constant f64:
                return f64.Value;

            case NullConstant:
                return null;

            case EnumConstant obj:
                return obj.Value;

            case IntegerConstant ic:
                switch (ic.Spec.Size)
                {
                    case 1:
                        return !ic.IsZero;

                    case 8:
                        if (ic.Spec.IsSigned)
                        {
                            return ic.ToInt8();
                        }

                        return ic.ToUInt8();

                    case 16:
                        if (ic.Spec.IsSigned)
                        {
                            return ic.ToInt16();
                        }

                        return ic.ToUInt16();

                    case 32:
                        if (ic.Spec.IsSigned)
                        {
                            return ic.ToInt32();
                        }

                        return ic.ToUInt32();

                    case 64:
                        if (ic.Spec.IsSigned)
                        {
                            return ic.ToInt64();
                        }

                        return ic.ToUInt64();
                }

                return null;
        }

        return null;
    }

    private static MethodInfo GetMatchingIntrinsicMethod(IMethod callee, Type intrinsicType)
    {
        var methods = intrinsicType.GetMethods().Where(_ => _.IsStatic)
            .Where(_ => _.Name.Equals(callee.Name.ToString(), StringComparison.OrdinalIgnoreCase));

        foreach (var m in methods)
        {
            if (MatchesParameters(callee, m.GetParameters().Select(_ => _.ParameterType).ToList()))
            {
                return m;
            }
        }

        return null;
    }

    private static bool MatchesParameters(IMethod method, List<Type> argTypes)
    {
        //ToDo: refactor to improve code
        var methodParams = string.Join(',', method.Parameters.Select(_ => _.Type.FullName.ToString()));
        var monocecilParams = string.Join(',', argTypes.Select(_ => _.FullName.ToString()));

        return methodParams.Equals(monocecilParams, StringComparison.Ordinal);
    }
}