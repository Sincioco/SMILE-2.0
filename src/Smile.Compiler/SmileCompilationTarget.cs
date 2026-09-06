namespace Smile.Compiler;

internal enum SmileCompilationTarget
{
    WindowsX64,
    Web,
    Library
}

internal sealed class CompilerOptions
{
    private readonly List<string> _sourcePaths = new();
    private readonly List<string> _libraryPaths = new();

    public string? SourcePath { get; private set; }
    public IReadOnlyList<string> SourcePaths => _sourcePaths;
    public IReadOnlyList<string> LibraryPaths => _libraryPaths;
    public string? ProjectPath { get; private set; }
    public string Configuration { get; private set; } = "Debug";
    public string? OutputPath { get; private set; }
    public string? OutputDirectory { get; private set; }
    public string? ApplicationId { get; private set; }
    public SmileCompilationTarget Target { get; private set; } = SmileCompilationTarget.WindowsX64;
    public bool KeepTemp { get; private set; }
    public bool EmitDebugInformation { get; private set; }
    public Smile.Language.SmileGraphicsBackend GraphicsBackend { get; private set; } = Smile.Language.SmileGraphicsBackend.Auto;
    public bool VSync { get; private set; } = true;
    public Smile.Language.SmileWebQuality WebQuality { get; private set; } = Smile.Language.SmileWebQuality.Full;

    public static bool TryParse(string[] args, out CompilerOptions options, out string? error)
    {
        options = new CompilerOptions();
        error = null;
        var targetSpecified = false;
        var graphicsSpecified = false;
        var vSyncSpecified = false;
        var webQualitySpecified = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--target", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || targetSpecified)
                {
                    error = "--target requires one value.";
                    return false;
                }
                targetSpecified = true;
                if (string.Equals(args[i], "windows-x64", StringComparison.OrdinalIgnoreCase))
                    options.Target = SmileCompilationTarget.WindowsX64;
                else if (string.Equals(args[i], "web", StringComparison.OrdinalIgnoreCase))
                    options.Target = SmileCompilationTarget.Web;
                else if (string.Equals(args[i], "library", StringComparison.OrdinalIgnoreCase))
                    options.Target = SmileCompilationTarget.Library;
                else
                {
                    error = "--target must be windows-x64, web, or library.";
                    return false;
                }
            }
            else if (string.Equals(args[i], "--output-dir", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || options.OutputDirectory != null)
                {
                    error = "--output-dir requires one directory.";
                    return false;
                }
                options.OutputDirectory = args[i];
            }
            else if (string.Equals(args[i], "--web-quality", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || webQualitySpecified ||
                    !Smile.Language.SmileWebDeployment.TryParseQuality(args[i], out var quality))
                {
                    error = "--web-quality requires one value: Full, Low, Medium, or High.";
                    return false;
                }
                webQualitySpecified = true;
                options.WebQuality = quality;
            }
            else if (string.Equals(args[i], "--keep-temp", StringComparison.OrdinalIgnoreCase))
            {
                options.KeepTemp = true;
            }
            else if (string.Equals(args[i], "--debug", StringComparison.OrdinalIgnoreCase))
            {
                options.EmitDebugInformation = true;
            }
            else if (string.Equals(args[i], "--source", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]))
                {
                    error = "--source requires one .smile source path.";
                    return false;
                }
                options._sourcePaths.Add(args[i]);
            }
            else if (string.Equals(args[i], "--library", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]))
                {
                    error = "--library requires one .smilelib package path.";
                    return false;
                }
                options._libraryPaths.Add(args[i]);
            }
            else if (string.Equals(args[i], "--project", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || options.ProjectPath != null || string.IsNullOrWhiteSpace(args[i]))
                {
                    error = "--project requires one .smileproj or .smilelibproj path.";
                    return false;
                }
                options.ProjectPath = args[i];
            }
            else if (string.Equals(args[i], "--configuration", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]))
                {
                    error = "--configuration requires one value.";
                    return false;
                }
                options.Configuration = args[i];
            }
            else if (string.Equals(args[i], "--application-id", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || options.ApplicationId != null)
                {
                    error = "--application-id requires one value.";
                    return false;
                }
                options.ApplicationId = args[i];
            }
            else if (string.Equals(args[i], "-o", StringComparison.OrdinalIgnoreCase))
            {
                if (++i >= args.Length || options.OutputPath != null)
                {
                    error = "-o requires one output file.";
                    return false;
                }
                options.OutputPath = args[i];
            }
            else if (string.Equals(args[i], "--graphics", StringComparison.OrdinalIgnoreCase))
            {
                graphicsSpecified = true;
                if (++i >= args.Length || !Enum.TryParse(args[i], true, out Smile.Language.SmileGraphicsBackend backend) ||
                    !Enum.IsDefined(backend))
                {
                    error = "--graphics must be Auto, GDI, or DirectX.";
                    return false;
                }
                options.GraphicsBackend = backend;
            }
            else if (string.Equals(args[i], "--vsync", StringComparison.OrdinalIgnoreCase))
            {
                vSyncSpecified = true;
                if (++i >= args.Length || !bool.TryParse(args[i], out var vSync))
                {
                    error = "--vsync must be true or false.";
                    return false;
                }
                options.VSync = vSync;
            }
            else if (options.SourcePath == null)
            {
                options.SourcePath = args[i];
            }
            else
            {
                error = $"Unexpected argument '{args[i]}'.";
                return false;
            }
        }

        if (options.SourcePath == null && options.ProjectPath == null)
        {
            error = "A startup .smile file or --project is required.";
            return false;
        }
        if (options.SourcePath != null && options.ProjectPath != null)
        {
            error = "A startup source and --project cannot be combined.";
            return false;
        }
        if (options.ProjectPath != null && (options._sourcePaths.Count != 0 || options._libraryPaths.Count != 0))
        {
            error = "--project reads sources and references from the project and cannot be combined with --source or --library.";
            return false;
        }

        if (webQualitySpecified && options.Target != SmileCompilationTarget.Web)
        {
            error = "--web-quality is available only for the Web target.";
            return false;
        }

        if (options.Target == SmileCompilationTarget.Library)
        {
            if (options.ProjectPath == null)
            {
                error = "The library target requires --project <library.smilelibproj>.";
                return false;
            }
            if (options.OutputDirectory != null || options.KeepTemp || options.EmitDebugInformation || graphicsSpecified || vSyncSpecified)
            {
                error = "The library target supports -o and --configuration only.";
                return false;
            }
            return true;
        }

        if (options.Target == SmileCompilationTarget.Web)
        {
            if (options.OutputDirectory == null)
            {
                error = "The Web target requires --output-dir <directory>.";
                return false;
            }
            if (options.OutputPath != null)
            {
                error = "The Web target uses --output-dir and cannot be combined with -o.";
                return false;
            }
            if (options.KeepTemp || options.EmitDebugInformation || graphicsSpecified || vSyncSpecified)
            {
                error = "--keep-temp, --debug, --graphics, and --vsync are available only for the windows-x64 target.";
                return false;
            }
        }
        else if (options.OutputDirectory != null)
        {
            error = "The windows-x64 target uses -o and cannot be combined with --output-dir.";
            return false;
        }

        return true;
    }
}
