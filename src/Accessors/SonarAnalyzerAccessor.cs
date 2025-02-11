using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Rules.CSharp;
using SonarRulesetTool.Models;

namespace SonarRulesetTool.Accessors;

internal static class SonarAnalyzerAccessor
{
    private static readonly Assembly SonarAssembly = typeof(AsyncVoidMethod).Assembly;

    /// <summary>
    /// Gets the version of SonarAnalyzer.CSharp.dll that this tool is built against.
    /// </summary>
    public static string GetVersion()
    {
        var versionAttribute = SonarAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return versionAttribute != null ? versionAttribute.InformationalVersion : $"v{SonarAssembly.GetName().Version}";
    }

    /// <summary>
    /// Retrieves the set of roslyn-based Sonar rules from SonarAnalyzer.CSharp.dll that run as part of 'dotnet build'.
    /// </summary>
    public static ISet<SonarRule> GetRulesFromAssembly()
    {
        var sonarRulesById = new Dictionary<string, SonarRule>();
        Type[] typesInSonarAssembly = SonarAssembly.GetExportedTypes();

        foreach (Type type in typesInSonarAssembly)
        {
            var analyzerAttribute = type.GetCustomAttribute<DiagnosticAnalyzerAttribute>();

            if (analyzerAttribute != null)
            {
                object? instance = Activator.CreateInstance(type);

                if (instance is DiagnosticAnalyzer analyzer)
                {
                    foreach (DiagnosticDescriptor? descriptor in analyzer.SupportedDiagnostics)
                    {
                        if (IsHiddenFromRuleSetEditor(descriptor))
                        {
                            continue;
                        }

                        SonarRule sonarRule = ToSonarRule(descriptor);

                        if (sonarRulesById.ContainsKey(sonarRule.Id))
                        {
                            // Some analyzers have multiple rule descriptors for a single Rule ID.

                            if (sonarRule.Severity == RuleSeverity.None)
                            {
                                // Skip this disabled one, in favor of the existing enabled descriptor.
                                continue;
                            }

                            if (sonarRulesById[sonarRule.Id].Severity == sonarRule.Severity)
                            {
                                // Skip duplicate descriptor with same severity as the existing one.
                                // From our perspective, they are identical.
                                continue;
                            }

                            if (sonarRulesById[sonarRule.Id].Severity != RuleSeverity.None)
                            {
                                throw new InvalidOperationException($"Multiple enabled rule descriptors found for {sonarRule.Id}.");
                            }
                        }

                        sonarRulesById[descriptor.Id] = sonarRule;
                    }
                }
            }
        }

        if (sonarRulesById.Count == 0)
        {
            Console.WriteLine($"WARNING: No Sonar rules found in '{SonarAssembly.Location}'.");
        }

        return sonarRulesById.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToHashSet();
    }

    private static bool IsHiddenFromRuleSetEditor(DiagnosticDescriptor descriptor)
    {
        // These rules do not show up in the Visual Studio RuleSet editor.
        return descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable);
    }

    private static SonarRule ToSonarRule(DiagnosticDescriptor descriptor)
    {
        RuleSeverity severity = !descriptor.IsEnabledByDefault
            ? RuleSeverity.None
            : descriptor.DefaultSeverity switch
            {
                DiagnosticSeverity.Hidden => RuleSeverity.Hidden,
                DiagnosticSeverity.Info => RuleSeverity.Info,
                DiagnosticSeverity.Warning => RuleSeverity.Warning,
                DiagnosticSeverity.Error => RuleSeverity.Error,
                _ => throw new NotSupportedException($"Failed to convert '{descriptor.DefaultSeverity}'.")
            };

        return new SonarRule(descriptor.Id, severity, SonarRule.SonarAnalyzerRepositoryKey);
    }
}
