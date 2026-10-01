using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Smile.VisualStudio;

// Drain each pipe continuously while retaining the complete text for diagnostics.
internal static class CompilerOutput
{
    internal static async Task<string> ReadAsync(StreamReader reader, Action<string>? report)
    {
        if (report == null)
            return await reader.ReadToEndAsync().ConfigureAwait(false);

        var captured = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
        {
            captured.AppendLine(line);
            report(line + "\r\n");
        }
        return captured.ToString();
    }
}
