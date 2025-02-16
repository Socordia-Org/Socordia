﻿using Flo;

namespace Backlang.Driver;

public class CompilerDriver
{
    public static async void Compile(CompilerContext context)
    {
        if (string.IsNullOrEmpty(context.TempOutputPath))
        {
            context.TempOutputPath = Environment.CurrentDirectory;
        }

        var hasError = (List<Message> messages) => messages.Any(_ => _.Severity == MessageSeverity.Error);

        var pipeline = Pipeline.Build<CompilerContext, CompilerContext>(
            cfg => {
                cfg.When(_ => _.Options.WaitForDebugger, _ => {
                    _.Add<WaitForDebuggerStage>();
                });

                cfg.Add<ParsingStage>();
                cfg.Add<SemanticCheckStage>();

                cfg.When(_ => !hasError(_.Messages) && _.Options.OutputTree, _ => {
                    _.Add<EmitTreeStage>();
                });

                cfg.When(_ => !hasError(_.Messages), _ => {
                    _.Add<InitStage>();
                });

                cfg.When(_ => !hasError(_.Messages), _ => {
                    _.Add<ExpandMacrosStage>();
                });

                cfg.When(_ => !hasError(_.Messages), _ => {
                    _.Add<IntermediateStage>();
                });

                cfg.When(_ => !hasError(_.Messages), _ => {
                    _.Add<TypeInheritanceStage>();
                });

                cfg.When(_ => !hasError(_.Messages), _ => {
                    _.Add<ExpandImplementationStage>();
                });

                cfg.When(_ => !hasError(_.Messages), _ => {
                    _.Add<ImplementationStage>();
                });

                cfg.When(_ => !hasError(_.Messages), _ => {
                    _.Add<InitEmbeddedResourcesStage>();
                });

                cfg.When(_ => !hasError(_.Messages), _ => {
                    _.Add<CompileTargetStage>();
                });

                cfg.When(_ => _.Messages.Any(), _ => {
                    _.Add<ReportErrorStage>();
                });
            }
        );

        await pipeline.Invoke(context);
    }
}