using Backlang.CodeAnalysis.AST;
using Backlang.Codeanalysis.Core;
using Loyc.Syntax;

namespace Backlang.Codeanalysis.Parsing;

public sealed partial class Parser
{
    public readonly List<Message> Messages;

    public Parser(SourceFile<StreamCharSource> document, List<Token> tokens, List<Message> messages)
    {
        Document = document;
        Iterator = new TokenIterator(tokens, document);
        Messages = messages;

        InitParsePoints();
    }

    public SourceFile<StreamCharSource> Document { get; }

    public TokenIterator Iterator { get; set; }

    public static CompilationUnit Parse(SourceDocument src)
    {
        SourceFile<StreamCharSource> document = src;

        if (document.Text == null)
        {
            return new CompilationUnit
            {
                Declarations = [],
                Messages = [Message.Error(ErrorID.EmptyFile, SourceRange.Synthetic)],
                Document = document
            };
        }

        var lexer = new Lexer();
        var tokens = lexer.Tokenize(document);

        var parser = new Parser(document, tokens, lexer.Messages);

        return parser.Program();
    }

    public CompilationUnit Program()
    {
        var node = Start();

        Iterator.Match(TokenType.EOF);

        return node;
    }

    private CompilationUnit Start()
    {
        var cu = new CompilationUnit();

        var body = InvokeDeclarationParsePoints();

        cu.Messages = Messages.Concat(Iterator.Messages).ToList();
        cu.Declarations = body;
        cu.Document = Document;

        return cu;
    }
}