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
    FillRectangleOpacity,
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
        ExpressionSyntax? textExpression, bool centered, int end)
    {
        Keyword = keyword;
        Operation = operation;
        Arguments = arguments;
        TextExpression = textExpression;
        Centered = centered;
        _end = end;
    }

    private readonly int _end;
    public SyntaxToken Keyword { get; }
    public GraphicsOperation Operation { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public ExpressionSyntax? TextExpression { get; }
    public bool Centered { get; }
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Span.Start, _end);
}

public enum ImageFilter
{
    Smooth,
    Pixel
}

[System.Flags]
public enum ImageFlip
{
    None = 0,
    Horizontal = 1,
    Vertical = 2
}

public sealed class DrawImageStatementSyntax : StatementSyntax
{
    public DrawImageStatementSyntax(SyntaxToken drawKeyword, SyntaxToken imageKeyword, ExpressionSyntax image,
        ExpressionSyntax? sourceX, ExpressionSyntax? sourceY, ExpressionSyntax? sourceWidth, ExpressionSyntax? sourceHeight,
        ExpressionSyntax destinationX, ExpressionSyntax destinationY, ExpressionSyntax? destinationWidth,
        ExpressionSyntax? destinationHeight, ExpressionSyntax? opacity, ImageFilter filter, ImageFlip flip,
        ExpressionSyntax? anchorX, ExpressionSyntax? anchorY, int end)
    {
        DrawKeyword = drawKeyword;
        ImageKeyword = imageKeyword;
        Image = image;
        SourceX = sourceX;
        SourceY = sourceY;
        SourceWidth = sourceWidth;
        SourceHeight = sourceHeight;
        DestinationX = destinationX;
        DestinationY = destinationY;
        DestinationWidth = destinationWidth;
        DestinationHeight = destinationHeight;
        Opacity = opacity;
        Filter = filter;
        Flip = flip;
        AnchorX = anchorX;
        AnchorY = anchorY;
        _end = end;
    }

    private readonly int _end;
    public SyntaxToken DrawKeyword { get; }
    public SyntaxToken ImageKeyword { get; }
    public ExpressionSyntax Image { get; }
    public ExpressionSyntax? SourceX { get; }
    public ExpressionSyntax? SourceY { get; }
    public ExpressionSyntax? SourceWidth { get; }
    public ExpressionSyntax? SourceHeight { get; }
    public ExpressionSyntax DestinationX { get; }
    public ExpressionSyntax DestinationY { get; }
    public ExpressionSyntax? DestinationWidth { get; }
    public ExpressionSyntax? DestinationHeight { get; }
    public ExpressionSyntax? Opacity { get; }
    public ImageFilter Filter { get; }
    public ImageFlip Flip { get; }
    public ExpressionSyntax? AnchorX { get; }
    public ExpressionSyntax? AnchorY { get; }
    public override TextSpan Span => TextSpan.FromBounds(DrawKeyword.Span.Start, _end);
}

public sealed class ImageLoadStatementSyntax : StatementSyntax
{
    public ImageLoadStatementSyntax(SyntaxToken keyword, SyntaxToken imageKeyword,
        AssignmentTargetSyntax target, ExpressionSyntax? path)
    {
        Keyword = keyword;
        ImageKeyword = imageKeyword;
        Target = target;
        Path = path;
    }

    public SyntaxToken Keyword { get; }
    public SyntaxToken ImageKeyword { get; }
    public AssignmentTargetSyntax Target { get; }
    public ExpressionSyntax? Path { get; }
    public bool IsUnload => Keyword.Kind == SyntaxKind.UnloadKeyword;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Span.Start, Path?.Span.End ?? Target.Span.End);
}

public sealed class ClipRectangleStatementSyntax : StatementSyntax
{
    public ClipRectangleStatementSyntax(SyntaxToken clipKeyword, IReadOnlyList<ExpressionSyntax> arguments,
        IReadOnlyList<StatementSyntax> statements, SyntaxToken endKeyword, SyntaxToken finalClipKeyword)
    {
        ClipKeyword = clipKeyword;
        Arguments = arguments;
        Statements = statements;
        EndKeyword = endKeyword;
        FinalClipKeyword = finalClipKeyword;
    }

    public SyntaxToken ClipKeyword { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken EndKeyword { get; }
    public SyntaxToken FinalClipKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(ClipKeyword.Span.Start, FinalClipKeyword.Span.End);
}

public sealed class DataLoadStatementSyntax : StatementSyntax
{
    public DataLoadStatementSyntax(SyntaxToken loadKeyword, SyntaxToken dataKeyword, ExpressionSyntax key,
        SyntaxToken destination, AssignmentTargetSyntax countTarget)
    {
        LoadKeyword = loadKeyword;
        DataKeyword = dataKeyword;
        Key = key;
        Destination = destination;
        CountTarget = countTarget;
    }

    public SyntaxToken LoadKeyword { get; }
    public SyntaxToken DataKeyword { get; }
    public ExpressionSyntax Key { get; }
    public SyntaxToken Destination { get; }
    public AssignmentTargetSyntax CountTarget { get; }
    public override TextSpan Span => TextSpan.FromBounds(LoadKeyword.Span.Start, CountTarget.Span.End);
}

public sealed class DataSaveStatementSyntax : StatementSyntax
{
    public DataSaveStatementSyntax(SyntaxToken saveKeyword, SyntaxToken dataKeyword, SyntaxToken source,
        ExpressionSyntax count, ExpressionSyntax key)
    {
        SaveKeyword = saveKeyword;
        DataKeyword = dataKeyword;
        Source = source;
        Count = count;
        Key = key;
    }

    public SyntaxToken SaveKeyword { get; }
    public SyntaxToken DataKeyword { get; }
    public SyntaxToken Source { get; }
    public ExpressionSyntax Count { get; }
    public ExpressionSyntax Key { get; }
    public override TextSpan Span => TextSpan.FromBounds(SaveKeyword.Span.Start, Key.Span.End);
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
    public SoundStatementSyntax(SyntaxToken keyword, SyntaxToken soundKeyword, SyntaxToken? path,
        ExpressionSyntax? channel)
    {
        Keyword = keyword;
        SoundKeyword = soundKeyword;
        Path = path;
        Channel = channel;
    }

    public SyntaxToken Keyword { get; }
    public SyntaxToken SoundKeyword { get; }
    public SyntaxToken? Path { get; }
    public ExpressionSyntax? Channel { get; }
    public bool IsStop => Keyword.Kind == SyntaxKind.StopKeyword;
    public override TextSpan Span => TextSpan.FromBounds(Keyword.Span.Start, Channel?.Span.End ?? Path?.Span.End ?? SoundKeyword.Span.End);
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
        ExpressionSyntax path, SyntaxToken intoKeyword, SyntaxToken destination, SyntaxToken countKeyword,
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
    public ExpressionSyntax Path { get; }
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
