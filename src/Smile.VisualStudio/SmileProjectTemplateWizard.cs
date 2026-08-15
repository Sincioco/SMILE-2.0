using System;
using System.Collections.Generic;
using System.Globalization;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

namespace Smile.VisualStudio;

public sealed class SmileProjectTemplateWizard : IWizard
{
    private const int HeaderValueWidth = 69;

    public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary,
        WizardRunKind runKind, object[] customParams)
    {
        if (replacementsDictionary == null)
            throw new ArgumentNullException(nameof(replacementsDictionary));

        var user = replacementsDictionary.TryGetValue("$username$", out var templateUser) &&
                   !string.IsNullOrWhiteSpace(templateUser)
            ? templateUser
            : Environment.UserName;
        var date = DateTime.Now.ToString("D", CultureInfo.CurrentCulture);
        var version = typeof(SmileProjectTemplateWizard).Assembly.GetName().Version;
        var smileVersion = version == null
            ? "2.0"
            : version.Major + "." + version.Minor + "." + version.Build;

        replacementsDictionary["$smileuser$"] = FitHeaderValue(user);
        replacementsDictionary["$smiledate$"] = FitHeaderValue(date);
        replacementsDictionary["$smileversion$"] = smileVersion;
    }

    public bool ShouldAddProjectItem(string filePath) => true;

    public void BeforeOpeningFile(ProjectItem projectItem)
    {
    }

    public void ProjectFinishedGenerating(Project project)
    {
    }

    public void ProjectItemFinishedGenerating(ProjectItem projectItem)
    {
    }

    public void RunFinished()
    {
    }

    private static string FitHeaderValue(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length > HeaderValueWidth)
            normalized = normalized.Substring(0, HeaderValueWidth);
        return normalized.PadRight(HeaderValueWidth);
    }
}
