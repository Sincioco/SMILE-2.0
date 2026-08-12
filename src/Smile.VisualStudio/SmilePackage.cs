using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace Smile.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideProjectFactory(typeof(SmileProjectFactory), "SMILE 2.0", "SMILE Project Files (*.smileproj;*.smilelibproj);*.smileproj;*.smilelibproj",
    "smileproj", "smileproj;smilelibproj", "LegacyProjectTemplates", LanguageVsTemplate = "Smile",
    NewProjectRequireNewFolderVsTemplate = true)]
[Guid(PackageGuidString)]
public sealed class SmilePackage : AsyncPackage
{
    public const string PackageGuidString = "9266c94d-1ac8-43d8-8804-8a59094aa1c4";

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        SmileBuildService.Initialize(this);
        RegisterProjectFactory(new SmileProjectFactory(this));
        await BuildSmileFileCommand.InitializeAsync(this);
    }
}
