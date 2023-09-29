using System.Xml.Linq;
using SonarRulesetTool.Models;

namespace SonarRulesetTool.Accessors;

internal static class SonarRuleSetAccessor
{
    private static readonly XName RuleSetElementName = XName.Get("RuleSet");
    private static readonly XName AnalyzerIdAttributeName = XName.Get("AnalyzerId");
    private static readonly XName RuleElementName = XName.Get("Rule");
    private static readonly XName IdAttributeName = XName.Get("Id");
    private static readonly XName ActionAttributeName = XName.Get("Action");

    /// <summary>
    /// Retrieves the set of roslyn-based Sonar rules from a .ruleset file.
    /// </summary>
    public static ISet<SonarRule> Read(string sourcePath)
    {
        var sonarRulesById = new Dictionary<string, SonarRule>();

        XDocument document = XDocument.Load(sourcePath);

        foreach (XElement ruleElement in GetRuleElements(document))
        {
            SonarRule? sonarRule = ToSonarRule(ruleElement);

            // On duplicates IDs, the first entry wins.
            if (sonarRule != null)
            {
                sonarRulesById.TryAdd(sonarRule.Id, sonarRule);
            }
        }

        if (!sonarRulesById.Any())
        {
            Console.WriteLine($"WARNING: No Sonar rules found in '{sourcePath}'.");
        }

        return sonarRulesById.Values.ToHashSet();
    }

    private static IEnumerable<XElement> GetRuleElements(XDocument document)
    {
        IEnumerable<XElement> rulesElements = document.Element(RuleSetElementName)?.Elements() ?? Array.Empty<XElement>();
        return rulesElements.Where(rulesElement => rulesElement.Attributes().Any(IsSonarRuleSet)).SelectMany(rulesElement => rulesElement.Elements());
    }

    private static bool IsSonarRuleSet(XAttribute attribute)
    {
        return attribute.Name == AnalyzerIdAttributeName && attribute.Value.StartsWith("SonarAnalyzer.CSharp", StringComparison.Ordinal);
    }

    private static SonarRule? ToSonarRule(XElement ruleElement)
    {
        if (ruleElement.Name == RuleElementName)
        {
            XAttribute? ruleId = ruleElement.Attribute(IdAttributeName);
            XAttribute? ruleActionText = ruleElement.Attribute(ActionAttributeName);

            if (ruleId != null && ruleActionText != null && Enum.TryParse(ruleActionText.Value, false, out RuleSeverity ruleAction))
            {
                return new SonarRule(ruleId.Value, ruleAction, SonarRule.SonarAnalyzerRepositoryKey);
            }
        }

        return null;
    }
}
