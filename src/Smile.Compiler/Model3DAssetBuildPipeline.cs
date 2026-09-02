using Smile.Language;

namespace Smile.Compiler;

internal sealed class Model3DAssetBuildAssets
{
    public required SmileProjectAssetManifest Manifest { get; init; }
    public IReadOnlyList<string> AssetPaths => Manifest.AssetPaths;
}

internal static class Model3DAssetBuildPipeline
{
    public static Model3DAssetBuildAssets Prepare(SmileProjectSourceSet project)
    {
        if (project.Model3DAssets.Items.Count == 0)
            return new Model3DAssetBuildAssets { Manifest = project.AssetManifest };

        var generated = new List<SmileProjectAssetItem>();
        foreach (var item in project.Model3DAssets.Items)
        {
            Model3DAssetCookResult result;
            try
            {
                result = Model3DAssetCooker.Cook(new Model3DAssetCookRequest
                {
                    ProjectDirectory = project.ProjectDirectory,
                    CacheRoot = Path.Combine(project.ProjectDirectory, "obj", "Smile", "Model3DCache"),
                    SourcePath = item.FullPath,
                    DescriptorPath = item.DescriptorPath,
                    LogicalPath = item.LogicalPath,
                    TextureOutputDirectory = item.TextureOutputDirectory,
                    Profile = item.Profile.ToString(),
                    Identity = item.Identity,
                    SampleRate = item.SampleRate,
                    ProductionState = item.ProductionState.ToString()
                });
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                              InvalidDataException or ArgumentException or FormatException or
                                              OverflowException)
            {
                throw new SmileProjectDiagnosticException("SML3712",
                    $"Model3DAsset '{item.Include}' cooking failed: {exception.Message}", project.ProjectPath,
                    item.Line, item.Column);
            }

            var status = result.Status switch
            {
                Model3DAssetCookStatus.CacheHit => "CACHE-HIT",
                Model3DAssetCookStatus.CacheRecovered => "CACHE-RECOVER",
                _ => "COOK"
            };
            Console.WriteLine($"{status} Model3DAsset {item.Include} -> {item.LogicalPath} [{result.CacheKey}]");
            foreach (var output in result.Outputs)
                generated.Add(new SmileProjectAssetItem(output.LogicalPath, output.FullPath,
                    Array.Empty<SmileProjectAssetInclude>()));
        }

        var allItems = project.AssetManifest.Items.Concat(generated)
            .OrderBy(item => item.LogicalPath, StringComparer.Ordinal).ToArray();
        var collision = SmileProjectAssetResolver.FindDestinationCollision(project.ProjectPath,
            allItems.Select(item => new KeyValuePair<string, string>(item.LogicalPath, item.FullPath)));
        if (collision != null)
            throw new SmileProjectDiagnosticException("SML3713",
                "Generated Model3DAsset output collides with another project asset. " + collision.Message,
                project.ProjectPath, collision.Line, collision.Column);

        var manifest = new SmileProjectAssetManifest(project.ProjectPath, project.AssetManifest.Includes,
            allItems, project.AssetManifest.Diagnostics);
        return new Model3DAssetBuildAssets { Manifest = manifest };
    }
}
