using SonarRulesetTool.Models;

namespace SonarRulesetTool.Combinators;

internal static class SonarRuleCombinator
{
    /// <summary>
    /// Applies ruleset overrides to the specified base ruleset.
    /// </summary>
    public static ISet<SonarRule> ApplyRuleSetOverride(ISet<SonarRule> baseRules, ISet<SonarRule> overrides)
    {
        Dictionary<string, SonarRule> sonarRulesById = baseRules.ToDictionary(rule => rule.Id);

        foreach (SonarRule rule in overrides)
        {
            sonarRulesById[rule.Id] = rule;
        }

        return sonarRulesById.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToHashSet();
    }
}
