using System;

namespace Smile.VisualStudio;

internal static class SmileProjectCommands
{
    public static readonly Guid CommandSet = new(SmileProjectFactory.SmileProjectTypeGuidString);
    public const int ProjectContextMenu = 0x2000;
    public const int SourceContextMenu = 0x2010;
    public const int FolderContextMenu = 0x2020;
    public const int ReferencesContextMenu = 0x2021;
    public const int ReferenceContextMenu = 0x2022;
    public const uint Build = 0x2100;
    public const uint Rebuild = 0x2101;
    public const uint Clean = 0x2102;
    public const uint AddNewSource = 0x2103;
    public const uint AddExistingSource = 0x2104;
    public const uint OpenProjectFolder = 0x2105;
    public const uint EditProjectFile = 0x2106;
    public const uint RefreshProject = 0x2107;
    public const uint AddReference = 0x2108;
    public const uint RemoveReference = 0x2109;
    public const uint SetStartupProject = 0x210A;
    public const uint SetStartupSource = 0x2110;
    public const uint IncludeSupportSource = 0x2111;
    public const uint RemoveSource = 0x2112;
    public const uint OpenContainingFolder = 0x2113;
    public const uint OpenFolder = 0x2120;
}
