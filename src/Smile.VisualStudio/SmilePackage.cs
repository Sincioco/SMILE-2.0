using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Smile.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideProjectFactory(typeof(SmileProjectFactory), "SMILE 2.0", "SMILE Project Files (*.smileproj;*.smilelibproj);*.smileproj;*.smilelibproj",
    "smileproj", "smileproj;smilelibproj", "LegacyProjectTemplates", LanguageVsTemplate = "Smile",
    NewProjectRequireNewFolderVsTemplate = true)]
[Guid(PackageGuidString)]
public sealed class SmilePackage : AsyncPackage, IVsSolutionEvents
{
    public const string PackageGuidString = "9266c94d-1ac8-43d8-8804-8a59094aa1c4";
    private DTE? _dte;
    private bool _newSolutionNeedsNativeDefault;
    private Microsoft.VisualStudio.Threading.JoinableTask? _nativeDefaultSelection;
    private IVsSolution? _solution;
    private uint _solutionEventsCookie;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var assembly = typeof(SmilePackage).Assembly;
        ActivityLog.LogInformation("SMILE 2.0", $"Loaded {assembly.Location} version {assembly.GetName().Version}.");
        SmileBuildService.Initialize(this);
        RegisterProjectFactory(new SmileProjectFactory(this));
        await BuildSmileFileCommand.InitializeAsync(this);

        var dte = await GetServiceAsync(typeof(SDTE)) as DTE;
        if (dte == null)
        {
            ActivityLog.LogWarning("SMILE 2.0", "Visual Studio automation service is unavailable; new solutions cannot select a native default automatically.");
            return;
        }
        _dte = dte;

        _solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
        if (_solution != null)
            ErrorHandler.ThrowOnFailure(_solution.AdviseSolutionEvents(this, out _solutionEventsCookie));
    }

    public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
    {
        _newSolutionNeedsNativeDefault = fNewSolution != 0;
        return VSConstants.S_OK;
    }

    public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (!IsSmileProject(pHierarchy) || (!_newSolutionNeedsNativeDefault && fAdded == 0))
            return VSConstants.S_OK;

        if (_dte?.Solution == null || _dte.Solution.Projects.Count != 1)
            return VSConstants.S_OK;

        _newSolutionNeedsNativeDefault = false;
        _nativeDefaultSelection = JoinableTaskFactory.RunAsync(SelectNativeDefaultWhenReadyAsync);
        return VSConstants.S_OK;
    }

    public int OnAfterCloseSolution(object pUnkReserved)
    {
        _newSolutionNeedsNativeDefault = false;
        return VSConstants.S_OK;
    }

    public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;
    public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;
    public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;
    public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
    public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
    public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
    public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

    protected override void Dispose(bool disposing)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (disposing && _solution != null && _solutionEventsCookie != 0)
        {
            _solution.UnadviseSolutionEvents(_solutionEventsCookie);
            _solutionEventsCookie = 0;
        }

        base.Dispose(disposing);
    }

    private async Task SelectNativeDefaultWhenReadyAsync()
    {
        try
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                if (attempt != 0)
                    await Task.Delay(250);

                await JoinableTaskFactory.SwitchToMainThreadAsync();
                if (_dte?.Solution == null || !_dte.Solution.IsOpen)
                    return;

                foreach (SolutionConfiguration configuration in _dte.Solution.SolutionBuild.SolutionConfigurations)
                {
                    foreach (SolutionContext context in configuration.SolutionContexts)
                    {
                        if (!string.Equals(context.PlatformName, SmileConfigurationProvider.NativePlatformName,
                                StringComparison.OrdinalIgnoreCase))
                            continue;

                        configuration.Activate();
                        ActivityLog.LogInformation("SMILE 2.0",
                            $"Selected new solution default platform '{SmileConfigurationProvider.NativePlatformName}'.");
                        return;
                    }
                }
            }

            ActivityLog.LogWarning("SMILE 2.0",
                $"New solution did not expose '{SmileConfigurationProvider.NativePlatformName}' in time to select it by default.");
        }
        catch (Exception exception)
        {
            ActivityLog.LogError("SMILE 2.0", $"Could not select the new solution default platform: {exception}");
        }
    }

    private static bool IsSmileProject(IVsHierarchy hierarchy)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var expected = new Guid(SmileProjectFactory.SmileProjectTypeGuidString);
        return ErrorHandler.Succeeded(hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT,
                   (int)__VSHPROPID.VSHPROPID_TypeGuid, out var actual)) && actual == expected;
    }
}
