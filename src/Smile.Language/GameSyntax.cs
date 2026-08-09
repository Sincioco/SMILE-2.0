using System.Collections.Generic;

namespace Smile.Language;

public sealed class GameWindowStatementSyntax : StatementSyntax
{
    public GameWindowStatementSyntax(SyntaxToken gameKeyword, SyntaxToken title, ExpressionSyntax? width, ExpressionSyntax? height, int end)
    {
        GameKeyword = gameKeyword;
        Title = title;
        Width = width;
        Height = height;
        _end = end;
    }

    private readonly int _end;
    public SyntaxToken GameKeyword { get; }
    public SyntaxToken Title { get; }
    public ExpressionSyntax? Width { get; }
    public ExpressionSyntax? Height { get; }
    public override TextSpan Span => TextSpan.FromBounds(GameKeyword.Span.Start, _end);
}

public sealed class ClearColorStatementSyntax : StatementSyntax
{
    public ClearColorStatementSyntax(SyntaxToken clearKeyword, ExpressionSyntax color)
    {
        ClearKeyword = clearKeyword;
        Color = color;
    }

    public SyntaxToken ClearKeyword { get; }
    public ExpressionSyntax Color { get; }
    public override TextSpan Span => TextSpan.FromBounds(ClearKeyword.Span.Start, Color.Span.End);
}

public enum GraphicsOperation
{
    FillRectangle,
    DrawRectangle,
    FillRoundedRectangle,
    DrawRoundedRectangle,
    FillCircle,
    DrawCircle,
    DrawArc,
    FillQuadrilateral,
    DrawQuadrilateral,
    DrawLine,
    DrawText,
    DrawNumber
}

public sealed class GraphicsStatementSyntax : StatementSyntax
{
    public GraphicsStatementSyntax(SyntaxToken keyword, GraphicsOperation operation, IReadOnlyList<ExpressionSyntax> arguments,
        SyntaxToken? text, bool centered, int end)
    {
        Keyword = keyword;
        Operation = operation;
        Arguments = arguments;
        Text = text;
        Centered = centered;
        _end = end;
    }

    private readonly int _end;
    public SyntaxToken Keyword { get; }
    public GraphicsOperation Operation { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken? Text { get; }
    public bool Centered { get; }
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Span.Start, _end);
}

public sealed class ShowScreenStatementSyntax : StatementSyntax
{
    public ShowScreenStatementSyntax(SyntaxToken showKeyword, SyntaxToken screenKeyword)
    {
        ShowKeyword = showKeyword;
        ScreenKeyword = screenKeyword;
    }

    public SyntaxToken ShowKeyword { get; }
    public SyntaxToken ScreenKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(ShowKeyword.Span.Start, ScreenKeyword.Span.End);
}

public sealed class SoundStatementSyntax : StatementSyntax
{
    public SoundStatementSyntax(SyntaxToken keyword, SyntaxToken soundKeyword, SyntaxToken? path)
    {
        Keyword = keyword;
        SoundKeyword = soundKeyword;
        Path = path;
    }

    public SyntaxToken Keyword { get; }
    public SyntaxToken SoundKeyword { get; }
    public SyntaxToken? Path { get; }
    public bool IsStop => Keyword.Kind == SyntaxKind.StopKeyword;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Span.Start, Path?.Span.End ?? SoundKeyword.Span.End);
}

public enum MusicOperation
{
    Play,
    Pause,
    Resume,
    Stop,
    SetVolume
}

public sealed class MusicStatementSyntax : StatementSyntax
{
    public MusicStatementSyntax(SyntaxToken keyword, SyntaxToken musicKeyword, MusicOperation operation,
        SyntaxToken? path, SyntaxToken? loopKeyword, ExpressionSyntax? volume)
    {
        Keyword = keyword;
        MusicKeyword = musicKeyword;
        Operation = operation;
        Path = path;
        LoopKeyword = loopKeyword;
        Volume = volume;
    }

    public SyntaxToken Keyword { get; }
    public SyntaxToken MusicKeyword { get; }
    public MusicOperation Operation { get; }
    public SyntaxToken? Path { get; }
    public SyntaxToken? LoopKeyword { get; }
    public ExpressionSyntax? Volume { get; }
    public bool Loop => LoopKeyword != null;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Span.Start,
        Volume?.Span.End ?? LoopKeyword?.Span.End ?? Path?.Span.End ?? MusicKeyword.Span.End);
}

public sealed class LoadStatementSyntax : StatementSyntax
{
    public LoadStatementSyntax(SyntaxToken loadKeyword, SyntaxToken identifier, SyntaxToken key, ExpressionSyntax defaultValue)
    {
        LoadKeyword = loadKeyword;
        Identifier = identifier;
        Key = key;
        DefaultValue = defaultValue;
    }

    public SyntaxToken LoadKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken Key { get; }
    public ExpressionSyntax DefaultValue { get; }
    public override TextSpan Span => TextSpan.FromBounds(LoadKeyword.Span.Start, DefaultValue.Span.End);
}

public sealed class TextFileLoadStatementSyntax : StatementSyntax
{
    public TextFileLoadStatementSyntax(SyntaxToken loadKeyword, SyntaxToken textKeyword, SyntaxToken fileKeyword,
        SyntaxToken path, SyntaxToken intoKeyword, SyntaxToken destination, SyntaxToken countKeyword,
        SyntaxToken countIdentifier)
    {
        LoadKeyword = loadKeyword;
        TextKeyword = textKeyword;
        FileKeyword = fileKeyword;
        Path = path;
        IntoKeyword = intoKeyword;
        Destination = destination;
        CountKeyword = countKeyword;
        CountIdentifier = countIdentifier;
    }

    public SyntaxToken LoadKeyword { get; }
    public SyntaxToken TextKeyword { get; }
    public SyntaxToken FileKeyword { get; }
    public SyntaxToken Path { get; }
    public SyntaxToken IntoKeyword { get; }
    public SyntaxToken Destination { get; }
    public SyntaxToken CountKeyword { get; }
    public SyntaxToken CountIdentifier { get; }
    public override TextSpan Span => TextSpan.FromBounds(LoadKeyword.Span.Start, CountIdentifier.Span.End);
}

public sealed class SaveStatementSyntax : StatementSyntax
{
    public SaveStatementSyntax(SyntaxToken saveKeyword, SyntaxToken identifier, SyntaxToken key)
    {
        SaveKeyword = saveKeyword;
        Identifier = identifier;
        Key = key;
    }

    public SyntaxToken SaveKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken Key { get; }
    public override TextSpan Span => TextSpan.FromBounds(SaveKeyword.Span.Start, Key.Span.End);
}
