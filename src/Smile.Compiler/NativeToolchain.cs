using System.Diagnostics;

namespace Smile.Compiler;

internal sealed class NativeToolchain
{
    public ToolchainResult AssembleAndLink(string assemblyPath, string objectPath, string outputPath, string runtimePath, bool isGame)
    {
        var installationPath = FindVisualStudio();
        if (installationPath == null)
            return new ToolchainResult(false, "Visual Studio with the C++ x64 tools was not found.");

        var vcvars = Path.Combine(installationPath, "VC", "Auxiliary", "Build", "vcvars64.bat");
        if (!File.Exists(vcvars))
            return new ToolchainResult(false, $"vcvars64.bat was not found under {installationPath}.");

        var command =
            $"call {Quote(vcvars)} >nul && " +
            $"ml64.exe /nologo /c /Fo{Quote(objectPath)} {Quote(assemblyPath)} && " +
            $"link.exe /nologo /subsystem:{(isGame ? "windows" : "console")} /entry:main /machine:x64 /out:{Quote(outputPath)} " +
            $"{Quote(objectPath)} {Quote(runtimePath)} kernel32.lib user32.lib gdi32.lib dwmapi.lib winmm.lib shell32.lib ole32.lib";

        return RunCommandPrompt(command);
    }

    private static string? FindVisualStudio()
    {
        var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (!File.Exists(vswhere))
            return null;

        var result = Run(vswhere, new[]
        {
            "-latest", "-products", "*", "-requires",
            "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-property", "installationPath"
        });
        return result.Success ? result.Output.Trim() : null;
    }

    private static ToolchainResult Run(string fileName, IEnumerable<string> arguments)
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

        using var process = Process.Start(startInfo);
        if (process == null)
            return new ToolchainResult(false, $"Could not start {fileName}.");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(standardOutput, standardError);
        var output = standardOutput.Result + standardError.Result;
        return new ToolchainResult(process.ExitCode == 0, output);
    }

    private static ToolchainResult RunCommandPrompt(string command)
    {
        var startInfo = new ProcessStartInfo("cmd.exe")
        {
            Arguments = "/d /s /c " + command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            return new ToolchainResult(false, "Could not start cmd.exe.");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(standardOutput, standardError);
        return new ToolchainResult(process.ExitCode == 0, standardOutput.Result + standardError.Result);
    }

    private static string Quote(string value) => "\"" + value + "\"";
}

internal sealed class ToolchainResult
{
    public ToolchainResult(bool success, string output)
    {
        Success = success;
        Output = output;
    }

    public bool Success { get; }
    public string Output { get; }
}
