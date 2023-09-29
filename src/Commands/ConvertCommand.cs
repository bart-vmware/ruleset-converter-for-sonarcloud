using System.CommandLine;
using System.Xml.Linq;
using SonarRulesetTool.Accessors;
using SonarRulesetTool.Combinators;
using SonarRulesetTool.Models;

namespace SonarRulesetTool.Commands;

internal static class ConvertCommand
{
    public static Command Register()
    {
        var ruleSetFileArgument = new Argument<FileInfo>("ruleSetFile", CommandArgumentParser.ParseFileInfo, false,
            "Path to the .ruleset file to read severities from");

        var sonarBuiltinFileArgument = new Argument<FileInfo>("sonarBuiltinFile", CommandArgumentParser.ParseFileInfo, false,
            "Path to the .xml file that contains the 'Sonar way' built-in profile");

        var outputFileNameOption = new Option<string?>("--outputFile", "Path to the output .xml file to import into SonarCloud afterwards");
        var profileNameOption = new Option<string?>("--profileName", "Name of the SonarCloud profile to use in the output file");
        var verboseOption = new Option<bool>("--verbose", "Displays the Rule IDs being processed");

        var command = new Command("convert", "Converts a .ruleset file containing SonarAnalyzer severities to SonarCloud")
        {
            ruleSetFileArgument,
            sonarBuiltinFileArgument,
            outputFileNameOption,
            profileNameOption,
            verboseOption
        };

        command.SetHandler(HandleCommand, ruleSetFileArgument, sonarBuiltinFileArgument, outputFileNameOption, profileNameOption, verboseOption);

        return command;
    }

    private static void HandleCommand(FileInfo ruleSetFileInfo, FileInfo sonarBuiltinFileInfo, string? outputFileName, string? profileName, bool isVerbose)
    {
        string parentDirectory = Path.GetDirectoryName(sonarBuiltinFileInfo.FullName)!;
        string sonarOutputPath = Path.Combine(parentDirectory, outputFileName ?? $"Sonar_{DateTime.Now:yyyyMMdd-HHmmss}.xml");
        string sonarProfileName = profileName ?? "SonarRuleSetAutoGenerated";

        GenerateSonarCloudXmlFile(ruleSetFileInfo, sonarBuiltinFileInfo, sonarOutputPath, sonarProfileName, isVerbose);
    }

    private static void GenerateSonarCloudXmlFile(FileInfo ruleSetFileInfo, FileInfo sonarBuiltinFileInfo, string sonarOutputPath, string targetProfileName,
        bool isVerbose)
    {
        // Get the default set of rules that run during solution build (from SonarAnalyzer.CSharp roslyn analyzer).
        string sonarAnalyzerVersion = SonarAnalyzerAccessor.GetVersion();
        ISet<SonarRule> defaultAnalyzerRules = SonarAnalyzerAccessor.GetRulesFromAssembly();
        PrintRuleSet(isVerbose, $"Built-in rules in SonarAnalyzer {sonarAnalyzerVersion}", defaultAnalyzerRules);

        // Get the set of rules from .ruleset file in solution.
        PrintFilePath(isVerbose, ruleSetFileInfo.FullName, "Reading SonarAnalyzer rules from");
        ISet<SonarRule> ruleSetOverrides = SonarRuleSetAccessor.Read(ruleSetFileInfo.FullName);
        PrintRuleSet(isVerbose, "Rules in .ruleset file", ruleSetOverrides);

        // Rules from .ruleset file override the default analyzer rules.
        ISet<SonarRule> analyzerRules = SonarRuleCombinator.ApplyRuleSetOverride(defaultAnalyzerRules, ruleSetOverrides);
        PrintRuleSet(isVerbose, "Effective analyzer rules", analyzerRules);

        // Get the built-in set of rules from SonarCloud (named "Sonar way").
        PrintFilePath(isVerbose, sonarBuiltinFileInfo.FullName, "Reading SonarCloud rules from");
        XDocument sourceXml = SonarCloudAccessor.Read(sonarBuiltinFileInfo.FullName);
        ISet<SonarRule> builtInRules = SonarCloudAccessor.ToRuleSet(sourceXml);
        PrintRuleSet(isVerbose, "Rules in SonarCloud profile", builtInRules);

        // Apply overrides from solution to SonarCloud rules. This preserves rules that only exist in SonarCloud.
        ISet<SonarRule> outputRules = SonarRuleCombinator.ApplyRuleSetOverride(builtInRules, analyzerRules);
        PrintRuleSet(isVerbose, "Effective SonarCloud rules", outputRules);

        // Convert the resulting set of rules back to XML.
        XDocument targetXml = SonarCloudAccessor.FromRuleSet(outputRules, targetProfileName);
        SonarCloudAccessor.SortByRuleKey(targetXml);

        // Write the result to disk, to be imported in SonarCloud.
        PrintFilePath(isVerbose, sonarOutputPath, "Writing SonarCloud rules to");
        SonarCloudAccessor.Write(targetXml, sonarOutputPath);
    }

    private static void PrintFilePath(bool isVerbose, string path, string title)
    {
        if (isVerbose)
        {
            Console.WriteLine($"{title}: {path}");
        }
    }

    private static void PrintRuleSet(bool isVerbose, string title, ISet<SonarRule> ruleSet)
    {
        if (isVerbose)
        {
            Console.WriteLine(title);

            foreach (SonarRule rule in ruleSet.OrderBy(rule => rule.Id))
            {
                Console.WriteLine($"  {rule}");
            }
        }
    }
}