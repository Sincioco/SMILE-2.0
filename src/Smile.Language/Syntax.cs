using System;
using System.Collections.Generic;

namespace Smile.Language;

public enum SyntaxKind
{
    BadToken,
    EndOfFileToken,
    NewLineToken,
    CommentToken,
    IdentifierToken,
    NumberToken,
    StringToken,

    PlusToken,
    MinusToken,
    StarToken,
    SlashToken,
    CommaToken,
    EqualsToken,
    NotEqualsToken,
    LessToken,
    GreaterToken,
    LessOrEqualsToken,
    GreaterOrEqualsToken,
    OpenParenthesisToken,
    CloseParenthesisToken,
    OpenBracketToken,
    CloseBracketToken,
    SemicolonToken,
    DotToken,
    ColonEqualsToken,

    DimKeyword,
    IfKeyword,
    ThenKeyword,
    ElseKeyword,
    EndKeyword,
    WithKeyword,
    ForKeyword,
    ToKeyword,
    DownKeyword,
    DoKeyword,
    LoopKeyword,
    UntilKeyword,
    PrintKeyword,
    GetKeyword,
    KeyKeyword,
    ClearKeyword,
    ScreenKeyword,
    WaitKeyword,
    MillisecondsKeyword,
    RandomKeyword,
    FromKeyword,
    TrueKeyword,
    FalseKeyword,
    AndKeyword,
    OrKeyword,
    NotKeyword,
    ConstKeyword,
    ModKeyword,
    SubKeyword,
    CallKeyword,
    FunctionKeyword,
    ReturnKeyword,
    SelectKeyword,
    CaseKeyword,
    ExitKeyword,
    ProgramKeyword,
    TimerKeyword,
    RgbKeyword,
    AbsKeyword,
    MinKeyword,
    MaxKeyword,
    GameClosedKeyword,
    WindowWidthKeyword,
    WindowHeightKeyword,
    KeyHeldKeyword,
    PointerXKeyword,
    PointerYKeyword,
    PointerDeltaXKeyword,
    PointerDeltaYKeyword,
    PointerWheelDeltaKeyword,
    PointerWheelRemainderKeyword,
    PointerInsideKeyword,
    PointerHeldKeyword,
    PointerPressedKeyword,
    PointerReleasedKeyword,
    ImageWidthKeyword,
    ImageHeightKeyword,
    ImageLoadedKeyword,
    TextWidthKeyword,
    TextHeightKeyword,
    TextLengthKeyword,
    TextCodeAtKeyword,
    TextSliceKeyword,
    Renderer3DKeyword,
    Renderer3DImageKeyword,
    Renderer3DTextKeyword,
    Renderer3DTextValueKeyword,
    GameKeyword,
    WindowKeyword,
    SizeKeyword,
    ByKeyword,
    FillKeyword,
    DrawKeyword,
    RectangleKeyword,
    RoundedKeyword,
    CircleKeyword,
    ArcKeyword,
    QuadrilateralKeyword,
    LineKeyword,
    TextKeyword,
    NumberKeyword,
    AtKeyword,
    ColorKeyword,
    CenteredKeyword,
    ShowKeyword,
    PlayKeyword,
    SoundKeyword,
    MusicKeyword,
    PauseKeyword,
    ResumeKeyword,
    VolumeKeyword,
    StopKeyword,
    LoadKeyword,
    FileKeyword,
    IntoKeyword,
    CountKeyword,
    SaveKeyword,
    DefaultKeyword,
    ModuleKeyword,
    ImportKeyword,
    AsKeyword,
    PublicKeyword,
    PrivateKeyword,
    OptionKeyword,
    ExplicitKeyword,
    BooleanKeyword,
    ByRefKeyword,
    ByValKeyword,
    TypeKeyword,
    EnumKeyword,
    PropertyKeyword,
    SetKeyword,
    MeKeyword,
    OptionalKeyword,
    ClassKeyword,
    NewKeyword,
    NothingKeyword,
    IsKeyword,
    ImageKeyword,
    UnloadKeyword,
    ClipKeyword,
    DataKeyword,
    OpacityKeyword,
    AnchorKeyword,
    FlipKeyword,
    HorizontalKeyword,
    VerticalKeyword,
    BothKeyword,
    FilterKeyword,
    SmoothKeyword,
    PixelKeyword,
    OnKeyword,
    ChannelKeyword,
    NoneKeyword,
    WKeyword,
    AKeyword,
    SKeyword,
    DKeyword,
    UpKeyword,
    LeftKeyword,
    RightKeyword,
    KeyNoneKeyword,
    KeyWKeyword,
    KeyAKeyword,
    KeySKeyword,
    KeyDKeyword,
    KeyOKeyword,
    KeyFKeyword,
    KeyGKeyword,
    KeyRKeyword,
    KeyUpKeyword,
    KeyDownKeyword,
    KeyLeftKeyword,
    KeyRightKeyword,
    KeyEnterKeyword,
    KeyEscapeKeyword,
    KeySpaceKeyword,
    Key1Keyword,
    Key2Keyword,
    Key3Keyword,
    Key4Keyword,
    KeyTabKeyword,
    KeyOtherKeyword,
    KeyPadAKeyword,
    KeyPadBKeyword,
    KeyPadXKeyword,
    KeyPadYKeyword,
    PointerPrimaryKeyword,
    PointerSecondaryKeyword,
    PointerMiddleKeyword,
    BlackKeyword,
    WhiteKeyword,
    RedKeyword,
    GreenKeyword,
    BlueKeyword,
    CyanKeyword,
    MagentaKeyword,
    YellowKeyword,
    OrangeKeyword,
    GrayKeyword,
    DarkRedKeyword,
    DarkGreenKeyword,
    DarkBlueKeyword,
    DarkGrayKeyword,
    LightRedKeyword,
    LightGreenKeyword,
    LightBlueKeyword,
    LightGrayKeyword,
    SoundChannelCountKeyword,
    DataBlockMaxBytesKeyword,
}

public static class SyntaxFacts
{
    private static readonly IReadOnlyList<string> NoParameters = Array.Empty<string>();
    private static readonly IReadOnlyList<string> ValueParameter = new[] { "value" };
    private static readonly IReadOnlyList<string> KeyParameter = new[] { "key" };
    private static readonly IReadOnlyList<string> TwoValueParameters = new[] { "first", "second" };
    private static readonly IReadOnlyList<string> ColorParameters = new[] { "red", "green", "blue" };
    private static readonly IReadOnlyList<string> ImageParameter = new[] { "image" };
    private static readonly IReadOnlyList<string> TextSizeParameters = new[] { "text", "size" };
    private static readonly IReadOnlyList<string> TextParameter = new[] { "text" };
    private static readonly IReadOnlyList<string> TextIndexParameters = new[] { "text", "index" };
    private static readonly IReadOnlyList<string> TextSliceParameters = new[] { "text", "start", "count" };
    private static readonly IReadOnlyList<string> Renderer3DParameters = new[]
    {
        "command", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j"
    };
    private static readonly IReadOnlyList<string> Renderer3DImageParameters = new[]
    {
        "command", "image", "a", "b", "c", "d", "e", "f", "g", "h"
    };
    private static readonly IReadOnlyList<string> Renderer3DTextParameters = new[]
    {
        "command", "text", "a", "b", "c", "d", "e", "f", "g", "h"
    };
    private static readonly IReadOnlyList<string> Renderer3DTextValueParameters = new[]
    {
        "command", "a", "b", "c", "d", "e", "f", "g", "h", "i"
    };

    private static readonly Dictionary<string, SyntaxKind> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dim"] = SyntaxKind.DimKeyword,
        ["If"] = SyntaxKind.IfKeyword,
        ["Then"] = SyntaxKind.ThenKeyword,
        ["Else"] = SyntaxKind.ElseKeyword,
        ["End"] = SyntaxKind.EndKeyword,
        ["With"] = SyntaxKind.WithKeyword,
        ["For"] = SyntaxKind.ForKeyword,
        ["To"] = SyntaxKind.ToKeyword,
        ["Down"] = SyntaxKind.DownKeyword,
        ["Do"] = SyntaxKind.DoKeyword,
        ["Loop"] = SyntaxKind.LoopKeyword,
        ["Until"] = SyntaxKind.UntilKeyword,
        ["Print"] = SyntaxKind.PrintKeyword,
        ["Get"] = SyntaxKind.GetKeyword,
        ["Key"] = SyntaxKind.KeyKeyword,
        ["Clear"] = SyntaxKind.ClearKeyword,
        ["Screen"] = SyntaxKind.ScreenKeyword,
        ["Wait"] = SyntaxKind.WaitKeyword,
        ["Milliseconds"] = SyntaxKind.MillisecondsKeyword,
        ["Random"] = SyntaxKind.RandomKeyword,
        ["From"] = SyntaxKind.FromKeyword,
        ["True"] = SyntaxKind.TrueKeyword,
        ["False"] = SyntaxKind.FalseKeyword,
        ["And"] = SyntaxKind.AndKeyword,
        ["Or"] = SyntaxKind.OrKeyword,
        ["Not"] = SyntaxKind.NotKeyword,
        ["Const"] = SyntaxKind.ConstKeyword,
        ["Mod"] = SyntaxKind.ModKeyword,
        ["Sub"] = SyntaxKind.SubKeyword,
        ["Call"] = SyntaxKind.CallKeyword,
        ["Function"] = SyntaxKind.FunctionKeyword,
        ["Return"] = SyntaxKind.ReturnKeyword,
        ["Select"] = SyntaxKind.SelectKeyword,
        ["Case"] = SyntaxKind.CaseKeyword,
        ["Exit"] = SyntaxKind.ExitKeyword,
        ["Program"] = SyntaxKind.ProgramKeyword,
        ["Timer"] = SyntaxKind.TimerKeyword,
        ["Rgb"] = SyntaxKind.RgbKeyword,
        ["Abs"] = SyntaxKind.AbsKeyword,
        ["Min"] = SyntaxKind.MinKeyword,
        ["Max"] = SyntaxKind.MaxKeyword,
        ["Game_Closed"] = SyntaxKind.GameClosedKeyword,
        ["Window_Width"] = SyntaxKind.WindowWidthKeyword,
        ["Window_Height"] = SyntaxKind.WindowHeightKeyword,
        ["Key_Held"] = SyntaxKind.KeyHeldKeyword,
        ["Pointer_X"] = SyntaxKind.PointerXKeyword,
        ["Pointer_Y"] = SyntaxKind.PointerYKeyword,
        ["Pointer_Delta_X"] = SyntaxKind.PointerDeltaXKeyword,
        ["Pointer_Delta_Y"] = SyntaxKind.PointerDeltaYKeyword,
        ["Pointer_Wheel_Delta"] = SyntaxKind.PointerWheelDeltaKeyword,
        ["Pointer_Wheel_Remainder"] = SyntaxKind.PointerWheelRemainderKeyword,
        ["Pointer_Inside"] = SyntaxKind.PointerInsideKeyword,
        ["Pointer_Held"] = SyntaxKind.PointerHeldKeyword,
        ["Pointer_Pressed"] = SyntaxKind.PointerPressedKeyword,
        ["Pointer_Released"] = SyntaxKind.PointerReleasedKeyword,
        ["Image_Width"] = SyntaxKind.ImageWidthKeyword,
        ["Image_Height"] = SyntaxKind.ImageHeightKeyword,
        ["Image_Loaded"] = SyntaxKind.ImageLoadedKeyword,
        ["Text_Width"] = SyntaxKind.TextWidthKeyword,
        ["Text_Height"] = SyntaxKind.TextHeightKeyword,
        ["Text_Length"] = SyntaxKind.TextLengthKeyword,
        ["Text_Code_At"] = SyntaxKind.TextCodeAtKeyword,
        ["Text_Slice"] = SyntaxKind.TextSliceKeyword,
        ["Renderer3D"] = SyntaxKind.Renderer3DKeyword,
        ["Renderer3DImage"] = SyntaxKind.Renderer3DImageKeyword,
        ["Renderer3DText"] = SyntaxKind.Renderer3DTextKeyword,
        ["Renderer3DTextValue"] = SyntaxKind.Renderer3DTextValueKeyword,
        ["Game"] = SyntaxKind.GameKeyword,
        ["Window"] = SyntaxKind.WindowKeyword,
        ["Size"] = SyntaxKind.SizeKeyword,
        ["By"] = SyntaxKind.ByKeyword,
        ["Fill"] = SyntaxKind.FillKeyword,
        ["Draw"] = SyntaxKind.DrawKeyword,
        ["Rectangle"] = SyntaxKind.RectangleKeyword,
        ["Rounded"] = SyntaxKind.RoundedKeyword,
        ["Circle"] = SyntaxKind.CircleKeyword,
        ["Arc"] = SyntaxKind.ArcKeyword,
        ["Quadrilateral"] = SyntaxKind.QuadrilateralKeyword,
        ["Line"] = SyntaxKind.LineKeyword,
        ["Text"] = SyntaxKind.TextKeyword,
        ["Number"] = SyntaxKind.NumberKeyword,
        ["At"] = SyntaxKind.AtKeyword,
        ["Color"] = SyntaxKind.ColorKeyword,
        ["Centered"] = SyntaxKind.CenteredKeyword,
        ["Show"] = SyntaxKind.ShowKeyword,
        ["Play"] = SyntaxKind.PlayKeyword,
        ["Sound"] = SyntaxKind.SoundKeyword,
        ["Music"] = SyntaxKind.MusicKeyword,
        ["Pause"] = SyntaxKind.PauseKeyword,
        ["Resume"] = SyntaxKind.ResumeKeyword,
        ["Volume"] = SyntaxKind.VolumeKeyword,
        ["Stop"] = SyntaxKind.StopKeyword,
        ["Load"] = SyntaxKind.LoadKeyword,
        ["File"] = SyntaxKind.FileKeyword,
        ["Into"] = SyntaxKind.IntoKeyword,
        ["Count"] = SyntaxKind.CountKeyword,
        ["Save"] = SyntaxKind.SaveKeyword,
        ["Default"] = SyntaxKind.DefaultKeyword,
        ["Module"] = SyntaxKind.ModuleKeyword,
        ["Import"] = SyntaxKind.ImportKeyword,
        ["As"] = SyntaxKind.AsKeyword,
        ["Public"] = SyntaxKind.PublicKeyword,
        ["Private"] = SyntaxKind.PrivateKeyword,
        ["Option"] = SyntaxKind.OptionKeyword,
        ["Explicit"] = SyntaxKind.ExplicitKeyword,
        ["Boolean"] = SyntaxKind.BooleanKeyword,
        ["ByRef"] = SyntaxKind.ByRefKeyword,
        ["ByVal"] = SyntaxKind.ByValKeyword,
        ["Type"] = SyntaxKind.TypeKeyword,
        ["Enum"] = SyntaxKind.EnumKeyword,
        ["Property"] = SyntaxKind.PropertyKeyword,
        ["Set"] = SyntaxKind.SetKeyword,
        ["Me"] = SyntaxKind.MeKeyword,
        ["Optional"] = SyntaxKind.OptionalKeyword,
        ["Class"] = SyntaxKind.ClassKeyword,
        ["New"] = SyntaxKind.NewKeyword,
        ["Nothing"] = SyntaxKind.NothingKeyword,
        ["Is"] = SyntaxKind.IsKeyword,
        ["Image"] = SyntaxKind.ImageKeyword,
        ["Unload"] = SyntaxKind.UnloadKeyword,
        ["Clip"] = SyntaxKind.ClipKeyword,
        ["Data"] = SyntaxKind.DataKeyword,
        ["Opacity"] = SyntaxKind.OpacityKeyword,
        ["Anchor"] = SyntaxKind.AnchorKeyword,
        ["Flip"] = SyntaxKind.FlipKeyword,
        ["Horizontal"] = SyntaxKind.HorizontalKeyword,
        ["Vertical"] = SyntaxKind.VerticalKeyword,
        ["Both"] = SyntaxKind.BothKeyword,
        ["Filter"] = SyntaxKind.FilterKeyword,
        ["Smooth"] = SyntaxKind.SmoothKeyword,
        ["Pixel"] = SyntaxKind.PixelKeyword,
        ["On"] = SyntaxKind.OnKeyword,
        ["Channel"] = SyntaxKind.ChannelKeyword,
        ["NONE"] = SyntaxKind.NoneKeyword,
        ["W"] = SyntaxKind.WKeyword,
        ["A"] = SyntaxKind.AKeyword,
        ["S"] = SyntaxKind.SKeyword,
        ["D"] = SyntaxKind.DKeyword,
        ["UP"] = SyntaxKind.UpKeyword,
        ["LEFT"] = SyntaxKind.LeftKeyword,
        ["RIGHT"] = SyntaxKind.RightKeyword,
        ["KEY_NONE"] = SyntaxKind.KeyNoneKeyword,
        ["KEY_W"] = SyntaxKind.KeyWKeyword,
        ["KEY_A"] = SyntaxKind.KeyAKeyword,
        ["KEY_S"] = SyntaxKind.KeySKeyword,
        ["KEY_D"] = SyntaxKind.KeyDKeyword,
        ["KEY_O"] = SyntaxKind.KeyOKeyword,
        ["KEY_F"] = SyntaxKind.KeyFKeyword,
        ["KEY_G"] = SyntaxKind.KeyGKeyword,
        ["KEY_R"] = SyntaxKind.KeyRKeyword,
        ["KEY_UP"] = SyntaxKind.KeyUpKeyword,
        ["KEY_DOWN"] = SyntaxKind.KeyDownKeyword,
        ["KEY_LEFT"] = SyntaxKind.KeyLeftKeyword,
        ["KEY_RIGHT"] = SyntaxKind.KeyRightKeyword,
        ["KEY_ENTER"] = SyntaxKind.KeyEnterKeyword,
        ["KEY_ESCAPE"] = SyntaxKind.KeyEscapeKeyword,
        ["KEY_SPACE"] = SyntaxKind.KeySpaceKeyword,
        ["KEY_1"] = SyntaxKind.Key1Keyword,
        ["KEY_2"] = SyntaxKind.Key2Keyword,
        ["KEY_3"] = SyntaxKind.Key3Keyword,
        ["KEY_4"] = SyntaxKind.Key4Keyword,
        ["KEY_TAB"] = SyntaxKind.KeyTabKeyword,
        ["KEY_OTHER"] = SyntaxKind.KeyOtherKeyword,
        ["KEY_PAD_A"] = SyntaxKind.KeyPadAKeyword,
        ["KEY_PAD_B"] = SyntaxKind.KeyPadBKeyword,
        ["KEY_PAD_X"] = SyntaxKind.KeyPadXKeyword,
        ["KEY_PAD_Y"] = SyntaxKind.KeyPadYKeyword,
        ["POINTER_PRIMARY"] = SyntaxKind.PointerPrimaryKeyword,
        ["POINTER_SECONDARY"] = SyntaxKind.PointerSecondaryKeyword,
        ["POINTER_MIDDLE"] = SyntaxKind.PointerMiddleKeyword,
        ["BLACK"] = SyntaxKind.BlackKeyword,
        ["WHITE"] = SyntaxKind.WhiteKeyword,
        ["RED"] = SyntaxKind.RedKeyword,
        ["GREEN"] = SyntaxKind.GreenKeyword,
        ["BLUE"] = SyntaxKind.BlueKeyword,
        ["CYAN"] = SyntaxKind.CyanKeyword,
        ["MAGENTA"] = SyntaxKind.MagentaKeyword,
        ["YELLOW"] = SyntaxKind.YellowKeyword,
        ["ORANGE"] = SyntaxKind.OrangeKeyword,
        ["GRAY"] = SyntaxKind.GrayKeyword,
        ["DARK_RED"] = SyntaxKind.DarkRedKeyword,
        ["DARK_GREEN"] = SyntaxKind.DarkGreenKeyword,
        ["DARK_BLUE"] = SyntaxKind.DarkBlueKeyword,
        ["DARK_GRAY"] = SyntaxKind.DarkGrayKeyword,
        ["LIGHT_RED"] = SyntaxKind.LightRedKeyword,
        ["LIGHT_GREEN"] = SyntaxKind.LightGreenKeyword,
        ["LIGHT_BLUE"] = SyntaxKind.LightBlueKeyword,
        ["LIGHT_GRAY"] = SyntaxKind.LightGrayKeyword,
        ["SOUND_CHANNEL_COUNT"] = SyntaxKind.SoundChannelCountKeyword,
        ["DATA_BLOCK_MAX_BYTES"] = SyntaxKind.DataBlockMaxBytesKeyword,
    };

    public static SyntaxKind GetKeywordKind(string text) =>
        Keywords.TryGetValue(text, out var kind) ? kind : SyntaxKind.IdentifierToken;

    public static IReadOnlyList<string> GetKeywordTexts() => new List<string>(Keywords.Keys);

    public static bool IsKeyword(SyntaxKind kind) =>
        (kind >= SyntaxKind.DimKeyword && kind <= SyntaxKind.OptionalKeyword) ||
        (kind >= SyntaxKind.ImageKeyword && kind <= SyntaxKind.ChannelKeyword);

    public static bool IsBuiltInConstant(SyntaxKind kind) =>
        kind >= SyntaxKind.NoneKeyword && kind <= SyntaxKind.DataBlockMaxBytesKeyword || kind == SyntaxKind.DownKeyword;

    public static bool IsBuiltInFunction(SyntaxKind kind) =>
        kind >= SyntaxKind.TimerKeyword && kind <= SyntaxKind.Renderer3DTextValueKeyword;

    public static IReadOnlyList<string> GetBuiltInFunctionParameters(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.TimerKeyword or SyntaxKind.GameClosedKeyword or SyntaxKind.WindowWidthKeyword or
                SyntaxKind.WindowHeightKeyword or SyntaxKind.PointerXKeyword or
                SyntaxKind.PointerYKeyword or SyntaxKind.PointerDeltaXKeyword or SyntaxKind.PointerDeltaYKeyword or
                SyntaxKind.PointerWheelDeltaKeyword or SyntaxKind.PointerWheelRemainderKeyword or
                SyntaxKind.PointerInsideKeyword => NoParameters,
            SyntaxKind.AbsKeyword => ValueParameter,
            SyntaxKind.KeyHeldKeyword => KeyParameter,
            SyntaxKind.PointerHeldKeyword or SyntaxKind.PointerPressedKeyword or SyntaxKind.PointerReleasedKeyword =>
                new[] { "button" },
            SyntaxKind.ImageWidthKeyword or SyntaxKind.ImageHeightKeyword or SyntaxKind.ImageLoadedKeyword => ImageParameter,
            SyntaxKind.TextWidthKeyword or SyntaxKind.TextHeightKeyword => TextSizeParameters,
            SyntaxKind.TextLengthKeyword => TextParameter,
            SyntaxKind.TextCodeAtKeyword => TextIndexParameters,
            SyntaxKind.TextSliceKeyword => TextSliceParameters,
            SyntaxKind.Renderer3DKeyword => Renderer3DParameters,
            SyntaxKind.Renderer3DImageKeyword => Renderer3DImageParameters,
            SyntaxKind.Renderer3DTextKeyword => Renderer3DTextParameters,
            SyntaxKind.Renderer3DTextValueKeyword => Renderer3DTextValueParameters,
            SyntaxKind.MinKeyword or SyntaxKind.MaxKeyword => TwoValueParameters,
            SyntaxKind.RgbKeyword => ColorParameters,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a built-in SMILE function.")
        };
    }

    public static string GetText(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.EndOfFileToken => "end of file",
            SyntaxKind.NewLineToken => "newline",
            SyntaxKind.IdentifierToken => "identifier",
            SyntaxKind.NumberToken => "number",
            SyntaxKind.StringToken => "text literal",
            SyntaxKind.PlusToken => "+",
            SyntaxKind.MinusToken => "-",
            SyntaxKind.StarToken => "*",
            SyntaxKind.SlashToken => "/",
            SyntaxKind.CommaToken => ",",
            SyntaxKind.EqualsToken => "=",
            SyntaxKind.NotEqualsToken => "<>",
            SyntaxKind.LessToken => "<",
            SyntaxKind.GreaterToken => ">",
            SyntaxKind.LessOrEqualsToken => "<=",
            SyntaxKind.GreaterOrEqualsToken => ">=",
            SyntaxKind.OpenParenthesisToken => "(",
            SyntaxKind.CloseParenthesisToken => ")",
            SyntaxKind.OpenBracketToken => "[",
            SyntaxKind.CloseBracketToken => "]",
            SyntaxKind.SemicolonToken => ";",
            SyntaxKind.DotToken => ".",
            SyntaxKind.ColonEqualsToken => ":=",
            _ when IsKeyword(kind) || IsBuiltInConstant(kind) => GetCanonicalKeywordText(kind),
            _ => kind.ToString()
        };
    }

    private static string GetCanonicalKeywordText(SyntaxKind kind)
    {
        foreach (var pair in Keywords)
        {
            if (pair.Value == kind)
                return pair.Key;
        }

        return kind.ToString().Replace("Keyword", string.Empty);
    }

    public static int GetUnaryPrecedence(SyntaxKind kind) =>
        kind == SyntaxKind.MinusToken || kind == SyntaxKind.NotKeyword ? 8 : 0;

    public static int GetBinaryPrecedence(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.ModKeyword => 7,
            SyntaxKind.PlusToken or SyntaxKind.MinusToken => 6,
            SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterOrEqualsToken => 5,
            SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or SyntaxKind.IsKeyword => 4,
            SyntaxKind.AndKeyword => 3,
            SyntaxKind.OrKeyword => 2,
            _ => 0
        };
    }

    public static long GetBuiltInConstantValue(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.NoneKeyword => 0,
            SyntaxKind.WKeyword => 1,
            SyntaxKind.AKeyword => 2,
            SyntaxKind.SKeyword => 3,
            SyntaxKind.DKeyword => 4,
            SyntaxKind.UpKeyword => 10,
            SyntaxKind.DownKeyword => 11,
            SyntaxKind.LeftKeyword => 12,
            SyntaxKind.RightKeyword => 13,
            SyntaxKind.KeyNoneKeyword => 0,
            SyntaxKind.KeyWKeyword => 1,
            SyntaxKind.KeyAKeyword => 2,
            SyntaxKind.KeySKeyword => 3,
            SyntaxKind.KeyDKeyword => 4,
            SyntaxKind.KeyOKeyword => 27,
            SyntaxKind.KeyFKeyword => 28,
            SyntaxKind.KeyGKeyword => 29,
            SyntaxKind.KeyRKeyword => 30,
            SyntaxKind.KeyUpKeyword => 10,
            SyntaxKind.KeyDownKeyword => 11,
            SyntaxKind.KeyLeftKeyword => 12,
            SyntaxKind.KeyRightKeyword => 13,
            SyntaxKind.KeyEnterKeyword => 14,
            SyntaxKind.KeyEscapeKeyword => 15,
            SyntaxKind.KeySpaceKeyword => 16,
            SyntaxKind.Key1Keyword => 17,
            SyntaxKind.Key2Keyword => 18,
            SyntaxKind.Key3Keyword => 20,
            SyntaxKind.Key4Keyword => 22,
            SyntaxKind.KeyTabKeyword => 21,
            SyntaxKind.KeyOtherKeyword => 19,
            SyntaxKind.KeyPadAKeyword => 23,
            SyntaxKind.KeyPadBKeyword => 24,
            SyntaxKind.KeyPadXKeyword => 25,
            SyntaxKind.KeyPadYKeyword => 26,
            SyntaxKind.PointerPrimaryKeyword => 1,
            SyntaxKind.PointerSecondaryKeyword => 2,
            SyntaxKind.PointerMiddleKeyword => 3,
            SyntaxKind.BlackKeyword => 0x000000,
            SyntaxKind.WhiteKeyword => 0xFFFFFF,
            SyntaxKind.RedKeyword => 0x0000FF,
            SyntaxKind.GreenKeyword => 0x00FF00,
            SyntaxKind.BlueKeyword => 0xFF0000,
            SyntaxKind.CyanKeyword => 0xFFFF00,
            SyntaxKind.MagentaKeyword => 0xFF00FF,
            SyntaxKind.YellowKeyword => 0x00FFFF,
            SyntaxKind.OrangeKeyword => 0x0080FF,
            SyntaxKind.GrayKeyword => 0x808080,
            SyntaxKind.DarkRedKeyword => 0x000080,
            SyntaxKind.DarkGreenKeyword => 0x008000,
            SyntaxKind.DarkBlueKeyword => 0x800000,
            SyntaxKind.DarkGrayKeyword => 0x404040,
            SyntaxKind.LightRedKeyword => 0x8080FF,
            SyntaxKind.LightGreenKeyword => 0x80FF80,
            SyntaxKind.LightBlueKeyword => 0xFF8080,
            SyntaxKind.LightGrayKeyword => 0xC0C0C0,
            SyntaxKind.SoundChannelCountKeyword => 16,
            SyntaxKind.DataBlockMaxBytesKeyword => 1024 * 1024,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}

public sealed class SyntaxToken
{
    private readonly int _spanLength;

    public SyntaxToken(SyntaxKind kind, int position, string text, object? value = null, int? spanLength = null)
    {
        Kind = kind;
        Position = position;
        Text = text;
        Value = value;
        _spanLength = spanLength ?? text.Length;
    }

    public SyntaxKind Kind { get; }
    public int Position { get; }
    public string Text { get; }
    public object? Value { get; }
    public TextSpan Span => new(Position, _spanLength);
}

public abstract class SyntaxNode
{
    public abstract TextSpan Span { get; }
}

public sealed class CompilationUnitSyntax : SyntaxNode
{
    public CompilationUnitSyntax(IReadOnlyList<StatementSyntax> statements, SyntaxToken endOfFileToken)
    {
        Statements = statements;
        EndOfFileToken = endOfFileToken;
    }

    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken EndOfFileToken { get; }
    public override TextSpan Span => Statements.Count == 0
        ? EndOfFileToken.Span
        : TextSpan.FromBounds(Statements[0].Span.Start, EndOfFileToken.Span.End);
}

public abstract class StatementSyntax : SyntaxNode { }
public abstract class ExpressionSyntax : SyntaxNode { }

public sealed class MeExpressionSyntax : ExpressionSyntax
{
    public MeExpressionSyntax(SyntaxToken meKeyword)
    {
        MeKeyword = meKeyword;
    }

    public SyntaxToken MeKeyword { get; }
    public override TextSpan Span => MeKeyword.Span;
}

public sealed class FieldAccessExpressionSyntax : ExpressionSyntax
{
    public FieldAccessExpressionSyntax(ExpressionSyntax receiver, SyntaxToken dotToken, SyntaxToken field)
    {
        Receiver = receiver;
        DotToken = dotToken;
        Field = field;
    }

    public ExpressionSyntax Receiver { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken Field { get; }
    public override TextSpan Span => TextSpan.FromBounds(Receiver.Span.Start, Field.Span.End);
}

public sealed class LeadingMemberAccessExpressionSyntax : ExpressionSyntax
{
    public LeadingMemberAccessExpressionSyntax(SyntaxToken dotToken, SyntaxToken member)
    {
        DotToken = dotToken;
        Member = member;
    }

    public SyntaxToken DotToken { get; }
    public SyntaxToken Member { get; }
    public override TextSpan Span => TextSpan.FromBounds(DotToken.Span.Start, Member.Span.End);
}

public sealed class AssignmentTargetSyntax : SyntaxNode
{
    public AssignmentTargetSyntax(ExpressionSyntax location)
    {
        Location = location;
    }

    public ExpressionSyntax Location { get; }
    public override TextSpan Span => Location.Span;
}

public sealed class AssignmentStatementSyntax : StatementSyntax
{
    public AssignmentStatementSyntax(AssignmentTargetSyntax target, SyntaxToken equalsToken, ExpressionSyntax expression)
    {
        Target = target;
        EqualsToken = equalsToken;
        Expression = expression;
    }

    public AssignmentTargetSyntax Target { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Expression { get; }
    public override TextSpan Span => TextSpan.FromBounds(Target.Span.Start, Expression.Span.End);
}

public sealed class DimStatementSyntax : StatementSyntax
{
    public DimStatementSyntax(SyntaxToken dimKeyword, SyntaxToken identifier, SyntaxToken? openBracket,
        IReadOnlyList<ExpressionSyntax> sizes, SyntaxToken? closeBracket, SyntaxToken? asKeyword = null,
        SyntaxToken? typeToken = null, NewExpressionSyntax? newInitializer = null)
    {
        DimKeyword = dimKeyword;
        Identifier = identifier;
        OpenBracket = openBracket;
        Sizes = sizes;
        CloseBracket = closeBracket;
        AsKeyword = asKeyword;
        TypeToken = typeToken;
        NewInitializer = newInitializer;
    }

    public SyntaxToken DimKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken? OpenBracket { get; }
    public IReadOnlyList<ExpressionSyntax> Sizes { get; }
    public SyntaxToken? CloseBracket { get; }
    public SyntaxToken? AsKeyword { get; }
    public SyntaxToken? TypeToken { get; }
    public NewExpressionSyntax? NewInitializer { get; }
    public bool IsArray => OpenBracket != null;
    public override TextSpan Span => TextSpan.FromBounds(DimKeyword.Span.Start,
        NewInitializer?.Span.End ?? TypeToken?.Span.End ?? CloseBracket?.Span.End ?? Identifier.Span.End);
}

public sealed class PrintStatementSyntax : StatementSyntax
{
    public PrintStatementSyntax(SyntaxToken printKeyword, IReadOnlyList<ExpressionSyntax> items, bool suppressNewLine, int end)
    {
        PrintKeyword = printKeyword;
        Items = items;
        SuppressNewLine = suppressNewLine;
        _end = end;
    }

    private readonly int _end;
    public SyntaxToken PrintKeyword { get; }
    public IReadOnlyList<ExpressionSyntax> Items { get; }
    public bool SuppressNewLine { get; }
    public override TextSpan Span => TextSpan.FromBounds(PrintKeyword.Span.Start, _end);
}

public sealed class GetKeyStatementSyntax : StatementSyntax
{
    public GetKeyStatementSyntax(SyntaxToken getKeyword, SyntaxToken keyKeyword, SyntaxToken identifier)
    {
        GetKeyword = getKeyword;
        KeyKeyword = keyKeyword;
        Identifier = identifier;
    }

    public SyntaxToken GetKeyword { get; }
    public SyntaxToken KeyKeyword { get; }
    public SyntaxToken Identifier { get; }
    public override TextSpan Span => TextSpan.FromBounds(GetKeyword.Span.Start, Identifier.Span.End);
}

public sealed class ClearScreenStatementSyntax : StatementSyntax
{
    public ClearScreenStatementSyntax(SyntaxToken clearKeyword, SyntaxToken screenKeyword)
    {
        ClearKeyword = clearKeyword;
        ScreenKeyword = screenKeyword;
    }

    public SyntaxToken ClearKeyword { get; }
    public SyntaxToken ScreenKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(ClearKeyword.Span.Start, ScreenKeyword.Span.End);
}

public sealed class WaitStatementSyntax : StatementSyntax
{
    public WaitStatementSyntax(SyntaxToken waitKeyword, ExpressionSyntax duration, SyntaxToken millisecondsKeyword)
    {
        WaitKeyword = waitKeyword;
        Duration = duration;
        MillisecondsKeyword = millisecondsKeyword;
    }

    public SyntaxToken WaitKeyword { get; }
    public ExpressionSyntax Duration { get; }
    public SyntaxToken MillisecondsKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(WaitKeyword.Span.Start, MillisecondsKeyword.Span.End);
}

public sealed class RandomStatementSyntax : StatementSyntax
{
    public RandomStatementSyntax(SyntaxToken randomKeyword, SyntaxToken identifier, SyntaxToken fromKeyword, ExpressionSyntax minimum, SyntaxToken toKeyword, ExpressionSyntax maximum)
    {
        RandomKeyword = randomKeyword;
        Identifier = identifier;
        FromKeyword = fromKeyword;
        Minimum = minimum;
        ToKeyword = toKeyword;
        Maximum = maximum;
    }

    public SyntaxToken RandomKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken FromKeyword { get; }
    public ExpressionSyntax Minimum { get; }
    public SyntaxToken ToKeyword { get; }
    public ExpressionSyntax Maximum { get; }
    public override TextSpan Span => TextSpan.FromBounds(RandomKeyword.Span.Start, Maximum.Span.End);
}

public sealed class IfClauseSyntax : SyntaxNode
{
    public IfClauseSyntax(ExpressionSyntax condition, IReadOnlyList<StatementSyntax> statements)
    {
        Condition = condition;
        Statements = statements;
    }

    public ExpressionSyntax Condition { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public override TextSpan Span => Statements.Count == 0 ? Condition.Span : TextSpan.FromBounds(Condition.Span.Start, Statements[Statements.Count - 1].Span.End);
}

public sealed class IfStatementSyntax : StatementSyntax
{
    public IfStatementSyntax(SyntaxToken ifKeyword, IReadOnlyList<IfClauseSyntax> clauses, IReadOnlyList<StatementSyntax> elseStatements, SyntaxToken endKeyword, SyntaxToken finalIfKeyword)
    {
        IfKeyword = ifKeyword;
        Clauses = clauses;
        ElseStatements = elseStatements;
        EndKeyword = endKeyword;
        FinalIfKeyword = finalIfKeyword;
    }

    public SyntaxToken IfKeyword { get; }
    public IReadOnlyList<IfClauseSyntax> Clauses { get; }
    public IReadOnlyList<StatementSyntax> ElseStatements { get; }
    public SyntaxToken EndKeyword { get; }
    public SyntaxToken FinalIfKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(IfKeyword.Span.Start, FinalIfKeyword.Span.End);
}

public sealed class WithStatementSyntax : StatementSyntax
{
    public WithStatementSyntax(SyntaxToken withKeyword, ExpressionSyntax target,
        IReadOnlyList<StatementSyntax> statements, SyntaxToken endKeyword, SyntaxToken finalWithKeyword)
    {
        WithKeyword = withKeyword;
        Target = target;
        Statements = statements;
        EndKeyword = endKeyword;
        FinalWithKeyword = finalWithKeyword;
    }

    public SyntaxToken WithKeyword { get; }
    public ExpressionSyntax Target { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken EndKeyword { get; }
    public SyntaxToken FinalWithKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(WithKeyword.Span.Start, FinalWithKeyword.Span.End);
}

public sealed class ForStatementSyntax : StatementSyntax
{
    public ForStatementSyntax(SyntaxToken forKeyword, SyntaxToken identifier, ExpressionSyntax lowerBound, bool isDescending, ExpressionSyntax upperBound, IReadOnlyList<StatementSyntax> statements, SyntaxToken finalForKeyword)
    {
        ForKeyword = forKeyword;
        Identifier = identifier;
        LowerBound = lowerBound;
        IsDescending = isDescending;
        UpperBound = upperBound;
        Statements = statements;
        FinalForKeyword = finalForKeyword;
    }

    public SyntaxToken ForKeyword { get; }
    public SyntaxToken Identifier { get; }
    public ExpressionSyntax LowerBound { get; }
    public bool IsDescending { get; }
    public ExpressionSyntax UpperBound { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken FinalForKeyword { get; }
    public override TextSpan Span => TextSpan.FromBounds(ForKeyword.Span.Start, FinalForKeyword.Span.End);
}

public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public LiteralExpressionSyntax(SyntaxToken literalToken, object value)
    {
        LiteralToken = literalToken;
        Value = value;
    }

    public SyntaxToken LiteralToken { get; }
    public object Value { get; }
    public override TextSpan Span => LiteralToken.Span;
}

public sealed class NothingExpressionSyntax : ExpressionSyntax
{
    public NothingExpressionSyntax(SyntaxToken nothingKeyword) => NothingKeyword = nothingKeyword;
    public SyntaxToken NothingKeyword { get; }
    public override TextSpan Span => NothingKeyword.Span;
}

public sealed class NameExpressionSyntax : ExpressionSyntax
{
    public NameExpressionSyntax(SyntaxToken identifier) => Identifier = identifier;
    public SyntaxToken Identifier { get; }
    public override TextSpan Span => Identifier.Span;
}

public sealed class ArrayAccessExpressionSyntax : ExpressionSyntax
{
    public ArrayAccessExpressionSyntax(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> indices, SyntaxToken closeBracket)
    {
        Identifier = identifier;
        Indices = indices;
        CloseBracket = closeBracket;
    }

    public SyntaxToken Identifier { get; }
    public IReadOnlyList<ExpressionSyntax> Indices { get; }
    public SyntaxToken CloseBracket { get; }
    public override TextSpan Span => TextSpan.FromBounds(Identifier.Span.Start, CloseBracket.Span.End);
}

public sealed class IndexedExpressionSyntax : ExpressionSyntax
{
    public IndexedExpressionSyntax(ExpressionSyntax receiver, SyntaxToken openBracket,
        IReadOnlyList<ExpressionSyntax> indices, SyntaxToken closeBracket)
    {
        Receiver = receiver;
        OpenBracket = openBracket;
        Indices = indices;
        CloseBracket = closeBracket;
    }

    public ExpressionSyntax Receiver { get; }
    public SyntaxToken OpenBracket { get; }
    public IReadOnlyList<ExpressionSyntax> Indices { get; }
    public SyntaxToken CloseBracket { get; }
    public override TextSpan Span => TextSpan.FromBounds(Receiver.Span.Start, CloseBracket.Span.End);
}

public sealed class UnaryExpressionSyntax : ExpressionSyntax
{
    public UnaryExpressionSyntax(SyntaxToken operatorToken, ExpressionSyntax operand)
    {
        OperatorToken = operatorToken;
        Operand = operand;
    }

    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Operand { get; }
    public override TextSpan Span => TextSpan.FromBounds(OperatorToken.Span.Start, Operand.Span.End);
}

public sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }

    public ExpressionSyntax Left { get; }
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Right { get; }
    public override TextSpan Span => TextSpan.FromBounds(Left.Span.Start, Right.Span.End);
}

public sealed class IdentityExpressionSyntax : ExpressionSyntax
{
    public IdentityExpressionSyntax(ExpressionSyntax left, SyntaxToken isKeyword, SyntaxToken? notKeyword,
        ExpressionSyntax right)
    {
        Left = left;
        IsKeyword = isKeyword;
        NotKeyword = notKeyword;
        Right = right;
    }

    public ExpressionSyntax Left { get; }
    public SyntaxToken IsKeyword { get; }
    public SyntaxToken? NotKeyword { get; }
    public ExpressionSyntax Right { get; }
    public bool IsNegated => NotKeyword != null;
    public override TextSpan Span => TextSpan.FromBounds(Left.Span.Start, Right.Span.End);
}

public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    public ParenthesizedExpressionSyntax(SyntaxToken openParenthesis, ExpressionSyntax expression, SyntaxToken closeParenthesis)
    {
        OpenParenthesis = openParenthesis;
        Expression = expression;
        CloseParenthesis = closeParenthesis;
    }

    public SyntaxToken OpenParenthesis { get; }
    public ExpressionSyntax Expression { get; }
    public SyntaxToken CloseParenthesis { get; }
    public override TextSpan Span => TextSpan.FromBounds(OpenParenthesis.Span.Start, CloseParenthesis.Span.End);
}
