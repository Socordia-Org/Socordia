using MrKWatkins.Ast.Listening;
using Socordia.CodeAnalysis.AST;
using Socordia.CodeAnalysis.AST.Expressions;

namespace SocordiaC.Compilation.Listeners.Body;

public class BinaryOperatorListener : Listener<BodyCompilation, AstNode, BinaryOperatorExpression>
{
    protected override void ListenToNode(BodyCompilation context, BinaryOperatorExpression node)
    {
        //Utils.CreateValue(node, context);
    }


    protected override bool ShouldListenToChildren(BodyCompilation context, BinaryOperatorExpression node)
    {
        return base.ShouldListenToChildren(context, node);
    }
}