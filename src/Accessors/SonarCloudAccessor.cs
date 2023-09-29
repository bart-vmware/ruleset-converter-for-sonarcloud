using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;
using SonarRulesetTool.Models;

namespace SonarRulesetTool.Accessors;

/// <summary>
/// Provides access to XML files imported/exported using SonarCloud and conversion to/from roslyn-based rule sets.
/// </summary>
internal static class SonarCloudAccessor
{
    private static readonly XName ProfileElementName = XName.Get("profile");
    private static readonly XName RulesElementName = XName.Get("rules");
    private static readonly XName RuleElementName = XName.Get("rule");
    private static readonly XName RepositoryKeyElementName = XName.Get("repositoryKey");
    private static readonly XName KeyElementName = XName.Get("key");
    private static readonly XName NameElementName = XName.Get("name");
    private static readonly XName LanguageElementName = XName.Get("language");

    public static XDocument Read(string sourcePath)
    {
        XDocument document = XDocument.Load(sourcePath);
        IEnumerable<XElement> ruleElements = GetRuleElements(document);

        if (!ruleElements.Any())
        {
            Console.WriteLine($"WARNING: No Sonar rules found in '{sourcePath}'.");
        }

        return document;
    }

    public static void Write(XDocument document, string targetPath)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true
        };

        using var writer = XmlWriter.Create(targetPath, settings);
        document.WriteTo(writer);
    }

    public static void SortByRuleKey(XDocument document)
    {
        var elementsByRuleKey = new Dictionary<string, XElement>();

        foreach (XElement ruleElement in GetRuleElements(document))
        {
            string? ruleKey = ruleElement.Element(KeyElementName)?.Value;
            string? repositoryKey = ruleElement.Element(RepositoryKeyElementName)?.Value;

            AssertRuleElementIsValid(ruleElement, ruleKey, repositoryKey, elementsByRuleKey);

            elementsByRuleKey[ruleKey] = ruleElement;
        }

        XElement? rulesElement = GetRulesElement(document);

        if (rulesElement != null)
        {
            rulesElement.RemoveAll();

            foreach (string ruleKey in elementsByRuleKey.Keys.OrderBy(key => key))
            {
                XElement ruleElement = elementsByRuleKey[ruleKey];
                rulesElement.Add(ruleElement);
            }
        }
    }

    private static IEnumerable<XElement> GetRuleElements(XDocument document)
    {
        XElement? rulesElement = GetRulesElement(document);
        return rulesElement?.Elements(RuleElementName) ?? Array.Empty<XElement>();
    }

    private static XElement? GetRulesElement(XDocument document)
    {
        return document.Element(ProfileElementName)?.Element(RulesElementName);
    }

    public static void Minimize(XDocument document)
    {
        var elementsByRuleKey = new Dictionary<string, XElement>();

        foreach (XElement ruleElement in GetRuleElements(document).ToArray())
        {
            string? ruleKey = ruleElement.Element(KeyElementName)?.Value;
            string? repositoryKey = ruleElement.Element(RepositoryKeyElementName)?.Value;

            AssertRuleElementIsValid(ruleElement, ruleKey, repositoryKey, elementsByRuleKey);

            // @formatter:keep_existing_linebreaks true
            var newRuleElement = new XElement(RuleElementName,
                new XElement(RepositoryKeyElementName, repositoryKey),
                new XElement(KeyElementName, ruleKey));
            // @formatter:keep_existing_linebreaks restore

            ruleElement.AddAfterSelf(newRuleElement);
            ruleElement.Remove();

            elementsByRuleKey[ruleKey] = newRuleElement;
        }
    }

    public static ISet<SonarRule> ToRuleSet(XDocument document)
    {
        var sonarRulesById = new Dictionary<string, SonarRule>();

        foreach (XElement ruleElement in GetRuleElements(document))
        {
            string? ruleKey = ruleElement.Element(KeyElementName)?.Value;
            string? repositoryKey = ruleElement.Element(RepositoryKeyElementName)?.Value;

            AssertRuleElementIsValid(ruleElement, ruleKey, repositoryKey, sonarRulesById);

            var sonarRule = new SonarRule(ruleKey, RuleSeverity.Warning, repositoryKey);
            sonarRulesById[sonarRule.Id] = sonarRule;
        }

        return sonarRulesById.Values.ToHashSet();
    }

    private static void AssertRuleElementIsValid<TValue>(XElement ruleElement, [NotNull] string? ruleKey, [NotNull] string? repositoryKey,
        Dictionary<string, TValue> seenRulesByKey)
    {
        // SonarCloud import fails on missing or duplicate keys.
        if (string.IsNullOrEmpty(ruleKey))
        {
            throw new InvalidOperationException($"Missing rule key in XML file at '{ruleElement}'.");
        }

        if (string.IsNullOrEmpty(repositoryKey))
        {
            throw new InvalidOperationException($"Missing rule repositoryKey in XML file at '{ruleElement}'.");
        }

        if (seenRulesByKey.ContainsKey(ruleKey))
        {
            throw new InvalidOperationException($"Duplicate rule key '{ruleKey}' in XML file at '{ruleElement}'.");
        }
    }

    public static XDocument FromRuleSet(ISet<SonarRule> ruleSet, string qualityProfileName)
    {
        var rulesElement = new XElement("rules");

        foreach (SonarRule rule in ruleSet)
        {
            if (rule.Severity != RuleSeverity.None && rule.Severity != RuleSeverity.Hidden)
            {
                // @formatter:keep_existing_linebreaks true
                rulesElement.Add(new XElement(RuleElementName,
                    new XElement(RepositoryKeyElementName, rule.RepositoryKey),
                    new XElement(KeyElementName, rule.Id)));
                // @formatter:keep_existing_linebreaks restore
            }
        }

        // @formatter:keep_existing_linebreaks true
        return new XDocument(new XElement(ProfileElementName,
            new XElement(NameElementName, qualityProfileName),
            new XElement(LanguageElementName, "cs"),
            rulesElement));
        // @formatter:keep_existing_linebreaks restore
    }
}
