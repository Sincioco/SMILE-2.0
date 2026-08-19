using System.Diagnostics;

namespace Smile.Compiler;

internal sealed class NativeToolchain
{
    internal static readonly TimeSpan VisualStudioDiscoveryTimeout = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan ToolchainTimeout = TimeSpan.FromMinutes(10);

    public ToolchainResult AssembleAndLink(string assemblyPath, string objectPath, string outputPath,
        string runtimePath, bool isGame, bool usesMusic, string? debugSourcePath, string? debugObjectPath,
        CancellationToken cancellationToken = default)
    {
        var discovery = FindVisualStudio(cancellationToken);
        if (!discovery.Success)
            return discovery;
        var installationPath = discovery.Output.Trim();

        var vcvars = Path.Combine(installationPath, "VC", "Auxiliary", "Build", "vcvars64.bat");
        if (!File.Exists(vcvars))
            return new ToolchainResult(false, $"vcvars64.bat was not found under {installationPath}.");

        var runtimeLibraries = " ucrt.lib msvcrt.lib msvcprt.lib vcruntime.lib";
        // The generated debug helper is debugger metadata, not the program entry point. The custom
        // /entry:main pipeline intentionally has no CRT startup to initialize a /GS security cookie,
        // so /GS- is constrained to this generated, buffer-free helper instead of pretending the
        // normal CRT protection is active.
        var debugCompile = debugSourcePath == null || debugObjectPath == null
            ? string.Empty
            : $"cl.exe /nologo /c /TC /utf-8 /Od /Z7 /JMC /GS- /Fo{Quote(debugObjectPath)} {Quote(debugSourcePath)} && ";
        var debugObject = debugObjectPath == null ? string.Empty : " " + Quote(debugObjectPath);
        var debugLink = debugObjectPath == null
            ? string.Empty
            : $" /debug:full /incremental:no /pdb:{Quote(Path.ChangeExtension(outputPath, ".pdb"))}";
        var command =
            $"call {Quote(vcvars)} >nul && " +
            debugCompile +
            $"ml64.exe /nologo /c /Fo{Quote(objectPath)} {Quote(assemblyPath)} && " +
            $"link.exe /nologo /subsystem:{(isGame ? "windows" : "console")} /entry:main /machine:x64 /out:{Quote(outputPath)}{debugLink} " +
            $"{Quote(objectPath)}{debugObject} {Quote(runtimePath)} kernel32.lib user32.lib gdi32.lib gdiplus.lib dwmapi.lib d3d11.lib d3dcompiler.lib dxgi.lib d2d1.lib dwrite.lib windowscodecs.lib winmm.lib shell32.lib ole32.lib windowsapp.lib xaudio2.lib{runtimeLibraries}";

        return RunCommandPrompt(command, cancellationToken);
    }

    private static ToolchainResult FindVisualStudio(CancellationToken cancellationToken)
    {
        var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (!File.Exists(vswhere))
            return new ToolchainResult(false, "Visual Studio with the C++ x64 tools was not found.",
                ProcessExecutionStatus.StartFailed);

        var result = Run(vswhere, new[]
        {
            "-latest", "-products", "*", "-requires",
            "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-property", "installationPath"
        }, VisualStudioDiscoveryTimeout, cancellationToken);
        if (!result.Success)
            return result;
        if (string.IsNullOrWhiteSpace(result.Output))
            return new ToolchainResult(false, "Visual Studio with the C++ x64 tools was not found.");
        return result;
    }

    private static ToolchainResult Run(string fileName, IEnumerable<string> arguments, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        return ToToolchainResult(BoundedProcessRunner.Run(startInfo, timeout, cancellationToken), fileName);
    }

    private static ToolchainResult RunCommandPrompt(string command, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("cmd.exe")
        {
            Arguments = "/d /s /c " + command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        return ToToolchainResult(BoundedProcessRunner.Run(startInfo, ToolchainTimeout, cancellationToken), "cmd.exe");
    }

    private static ToolchainResult ToToolchainResult(ProcessExecutionResult result, string command)
    {
        var output = result.CombinedOutput;
        return result.Status switch
        {
            ProcessExecutionStatus.Completed => new ToolchainResult(result.ExitCode == 0, output,
                result.Status),
            ProcessExecutionStatus.TimedOut => new ToolchainResult(false,
                $"{command} timed out after the configured limit.\n{output}", result.Status),
            ProcessExecutionStatus.Cancelled => new ToolchainResult(false,
                $"{command} was canceled.\n{output}", result.Status),
            _ => new ToolchainResult(false,
                $"Could not start {command}: {result.StartError}\n{output}", result.Status)
        };
    }

    private static string Quote(string value) => "\"" + value + "\"";
}

internal sealed class ToolchainResult
{
    public ToolchainResult(bool success, string output,
        ProcessExecutionStatus status = ProcessExecutionStatus.Completed)
    {
        Success = success;
        Output = output;
        Status = status;
    }

    public bool Success { get; }
    public string Output { get; }
    public ProcessExecutionStatus Status { get; }
}
