internal sealed class TestContext
{
    private readonly List<string> _failures = new();

    public IReadOnlyList<string> Failures => _failures;
    public int Passed { get; private set; }

    public void Run(string name, Action test)
    {
        try
        {
            test();
            Passed++;
        }
        catch (Exception exception)
        {
            _failures.Add($"{name}:{Environment.NewLine}{exception}");
        }
    }
}

internal static class RepositoryTestContext
{
    private const string RepositoryRootEnvironmentVariable = "SMILE_REPOSITORY_ROOT";

    public static string FindRepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable(RepositoryRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var configuredRoot = Path.GetFullPath(configured);
            if (IsRepositoryRoot(configuredRoot))
                return configuredRoot;
            throw new InvalidOperationException(
                $"{RepositoryRootEnvironmentVariable} does not identify the SMILE repository: {configuredRoot}");
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (IsRepositoryRoot(directory.FullName))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the SMILE repository from test output '{AppContext.BaseDirectory}'.");
    }

    private static bool IsRepositoryRoot(string path) =>
        File.Exists(Path.Combine(path, "SMILE 2.0.sln")) &&
        File.Exists(Path.Combine(path, "AGENTS.md"));
}
