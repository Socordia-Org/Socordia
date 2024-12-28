using System.Reflection;
using DistIL.AsmIO;
using DistIL.IR;
using Socordia.CodeAnalysis.AST;
using Socordia.CodeAnalysis.AST.Declarations;
using Socordia.CodeAnalysis.AST.Expressions;
using Socordia.CodeAnalysis.AST.Literals;
using Socordia.CodeAnalysis.AST.TypeNames;
using Socordia.CodeAnalysis.Parsing;
using SocordiaC.Compilation.Body;

namespace SocordiaC.Compilation;

public static class Utils
{
    public static TypeDesc? GetTypeFromNode(TypeName node, TypeDef containingType)
    {
        var resolvedType = GetTypeFromNodeImpl(node, containingType);

        if (resolvedType == null)
        {
            node.AddError($"Type '{node}' not found");
        }

        return resolvedType ?? PrimType.Void;
    }

    private static readonly Dictionary<string, TypeDesc> Primities = new()
    {
        ["none"]  = PrimType.Void,
        ["bool"]  = PrimType.Bool,
        ["i8" ] = PrimType.Byte,
        ["i16" ] = PrimType.Int16,
        ["i32" ] = PrimType.Int32,
        ["i64" ] = PrimType.Int64,
        ["f32" ] = PrimType.Single,
        ["f64" ] = PrimType.Double,
    };

    private static TypeDesc? GetTypeFromNodeImpl(TypeName node, TypeDef containingType)
    {
        if (node is UnitTypeName unitTypeName)
        {
            return GetTypeFromNodeImpl(unitTypeName.Unit, containingType);
        }

        if (node is SimpleTypeName id)
        {
            if (Primities.TryGetValue(id.Name, out var prim))
            {
                return prim;
            }

            var type = containingType.Module.FindType(containingType.Namespace, id.Name);
            if (type != null)
            {
                return type;
            }

            if (containingType.Name == id.Name)
            {
                return containingType;
            }
        }
        else if (node is QualifiedTypeName qname)
        {
            if (qname.Type is SimpleTypeName simple)
            {
                var type = containingType.Module.FindType(qname.Namespace, simple.Name);
                if (type != null)
                {
                    return type;
                }

                return containingType.Module.Resolver.FindType(qname.ToString());
            }
        }
        else if (node is PointerTypeName ptr)
        {
            var type = GetTypeFromNodeImpl(ptr.Type, containingType);
            if (type != null)
            {
                return ptr.Kind switch
                {
                    PointerKind.Transient => type.CreatePointer(),
                    PointerKind.Reference => type.CreateByref(),
                    _ => throw new InvalidOperationException("Invalid pointerkind")
                };
            }
        }

        return null;
    }

    public static TypeDesc? GetTypeFromNode(TypeName node, ModuleDef module)
    {
        if (node is NoTypeName)
        {
            return null;
        }

        if (node is SimpleTypeName id)
        {
            if (Primities.TryGetValue(id.Name, out var prim))
            {
                return prim;
            }
        }

        if (node is UnitTypeName unitTypeName)
        {
            return GetTypeFromNode(unitTypeName, module);
        }

        if (node is QualifiedTypeName qname)
        {
            if (qname.Type is SimpleTypeName simple)
            {
                var type = module.FindType(qname.Namespace, simple.Name);
                if (type != null)
                {
                    return type;
                }

                return (TypeDef)module.Resolver.FindType(qname.ToString());
            }
        }

        throw new Exception("cannot get type from node");
    }

    public static TypeAttributes GetModifiers(Declaration node)
    {
        var attrs = TypeAttributes.Public;

        foreach (var modifier in node.Modifiers)
        {
            attrs |= modifier switch
            {
                Modifier.Static => TypeAttributes.Sealed | TypeAttributes.Abstract,
                Modifier.Internal => TypeAttributes.NotPublic,
                Modifier.Public => TypeAttributes.Public,
                _ => throw new NotImplementedException()
            };
        }

        if (node.Modifiers.Contains(Modifier.Private) || node.Modifiers.Contains(Modifier.Internal))
        {
            attrs &= ~TypeAttributes.Public;
        }

        return attrs;
    }

    public static object? GetLiteralValue(AstNode? node)
    {
        if (node is LiteralNode lit)
        {
            return lit.Value;
        }

        return null;
    }

    public static Value CreateValue(AstNode valueNode, BodyCompilation compilation)
    {
        return valueNode switch
        {
            LiteralNode literal => CreateLiteral(literal.Value),
            DefaultLiteral def => CreateDefault(def, compilation),
            CallExpression call => CreateCall(call, compilation),
            _ => throw new NotImplementedException()
        };
    }

    private static Value CreateCall(CallExpression call, BodyCompilation compilation)
    {
        var listener = new CallExpressionListener(false);
        listener.Listen(compilation, call);

        return listener.CallInstruction;
    }

    private static Value CreateDefault(DefaultLiteral def, BodyCompilation compilation)
    {
        var type = GetTypeFromNode(def.Type, compilation.Driver.Compilation.Module)!;

        return compilation.Builder.CreateDefaultOf(type);
    }

    private static Value CreateLiteral(object literalValue)
    {
        return literalValue switch
        {
            bool b => ConstInt.Create(PrimType.Bool, b ? 1 : 0),
            byte by => ConstInt.CreateI(by),
            short s => ConstInt.CreateI(s),
            int i => ConstInt.CreateI(i),
            long l => ConstInt.CreateL(l),
            float f => ConstFloat.CreateD(f),
            double d => ConstFloat.CreateD(d),
            string str => ConstString.Create(str),
            char c => ConstInt.CreateI(c),
            null => ConstNull.Create(),
            _ => throw new NotImplementedException()
        };
    }

    public static bool IsUnitType(this TypeDef type)
    {
        var attribs = type.GetCustomAttribs();

        return attribs.Any(a => a.Type is { Namespace: "Socordia.Core.CompilerService", Name: "MeasureAttribute" });
    }
}