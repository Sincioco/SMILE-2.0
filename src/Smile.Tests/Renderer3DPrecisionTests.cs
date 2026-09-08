using Smile.Language;

internal static class Renderer3DPrecisionTests
{
    public static void Register(TestContext tests)
    {
        tests.Run("Fractional renderer bridge keeps capability, payload and result types", () =>
        {
            const string window = "Game Window \"Precision\" Size 640 By 480\n";
            const string mutation = "Print Renderer3DDouble(1, 0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 60.0, 0.1, 1000.0)\n";
            const string query = "Dim Value As Double\nValue = Renderer3DDoubleValue(1, 0, 0, 0)\n";
            Require(!SmileLanguage.Analyze(window + mutation + query).HasErrors, "Valid typed bridge");
            Require(SmileLanguage.Analyze(mutation).HasErrors, "Mutation requires Game Window");
            Require(SmileLanguage.Analyze(query).HasErrors, "Query requires Game Window");
            Require(SmileLanguage.Analyze(window + mutation.Replace("1, 0,", "1, 0.0,")).HasErrors,
                "Integral resource identity");
            Require(SmileLanguage.Analyze(window + mutation.Replace("60.0", "60")).HasErrors,
                "No implicit payload conversion");
            Require(SmileLanguage.Analyze(window + query.Replace("As Double", "As Number")).HasErrors,
                "No implicit query narrowing");
            Require(SmileLanguage.Analyze(window + query.Replace("(1, 0, 0, 0)", "(1, 0, 0.0, 0)")).HasErrors,
                "Integral query selector");
        });
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
