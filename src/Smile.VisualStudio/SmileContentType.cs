using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Smile.VisualStudio;

internal static class SmileContentType
{
    public const string Name = "SMILE 2.0";

#pragma warning disable 649
    [Export]
    [Name(Name)]
    [BaseDefinition("code")]
    internal static ContentTypeDefinition? Definition;

    [Export]
    [FileExtension(".smile")]
    [ContentType(Name)]
    internal static FileExtensionToContentTypeDefinition? FileExtension;
#pragma warning restore 649
}
