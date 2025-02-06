using DistIL.AsmIO;
using MrKWatkins.Ast.Listening;
using Socordia.CodeAnalysis.AST;
using Socordia.CodeAnalysis.AST.Declarations;
using Socordia.CodeAnalysis.AST.Declarations.DU;
using Socordia.Compilation;
using System.Reflection;

namespace SocordiaC.Compilation.Listeners;

public class CollectDUsListener : Listener<Driver, AstNode, DiscriminatedUnionDeclaration>
{
    protected override void ListenToNode(Driver context, DiscriminatedUnionDeclaration node)
    {
        var ns = context.GetNamespaceOf(node);
        var baseType =
            context.Compilation.Module.CreateType(ns, node.Name,
                Utils.GetTypeModifiers(node) | TypeAttributes.Abstract);
        baseType.AddCompilerGeneratedAttribute(context.Compilation);

        foreach (var child in node.Children.OfType<DiscriminatedType>())
        {
            var childType = baseType.CreateNestedType(child.Name,
                Utils.GetTypeModifiers(node), baseType: baseType);
            childType.AddCompilerGeneratedAttribute(context.Compilation);

            foreach (var parameter in child.Children.OfType<ParameterDeclaration>())
            {
                childType.CreateField(parameter.Name, new TypeSig(Utils.GetTypeFromNode(parameter.Type, baseType)),
                    Utils.GetFieldModifiers(parameter));
            }

            var ctor = CommonIR.GenerateCtor(childType);
            ctor.AddCompilerGeneratedAttribute(context.Compilation);
        }

        Utils.EmitAnnotations(node, baseType);
    }

    protected override bool ShouldListenToChildren(Driver context, DiscriminatedUnionDeclaration node)
    {
        return false;
    }
}