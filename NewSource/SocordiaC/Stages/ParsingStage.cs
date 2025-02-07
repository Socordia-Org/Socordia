using Flo;
using Socordia.CodeAnalysis.AST;
using SocordiaC.Compilation.Scoping;

namespace SocordiaC.Stages;

public sealed class ParsingStage : IHandler<Driver, Driver>
{
    public async Task<Driver> HandleAsync(Driver context,
        Func<Driver, Task<Driver>> next)
    {
        ParseSourceFiles(context);

        return await next.Invoke(context);
    }

    private static void ParseSourceFiles(Driver context)
    {
        foreach (var filename in context.Settings.Sources)
        {
            if (File.Exists(filename))
            {
                var tree = CompilationUnit.FromFile(filename);
                tree.Declarations.Tag = new Scope(null);

                ApplyTree(context, tree);
            }
        }
        // context.Messages.Add(Message.Error($"File '{filename}' does not exists", (TextFilePosition)TextFilePosition.None));
    }

    private static void ApplyTree(Driver context, CompilationUnit tree)
    {
        context.Trees.Add(tree);

        context.Messages.AddRange(tree.Messages);
    }
}