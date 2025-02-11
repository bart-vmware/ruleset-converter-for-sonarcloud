namespace SonarRulesetTool.Models;

internal sealed class SonarRule(string id, RuleSeverity severity, string repositoryKey) : IEquatable<SonarRule>
{
    /// <summary>
    /// The "repositoryKey" value used in SonarCloud XML files.
    /// </summary>
    public const string SonarAnalyzerRepositoryKey = "csharpsquid";

    public string RepositoryKey { get; } = repositoryKey;
    public string Id { get; } = id;
    public RuleSeverity Severity { get; } = severity;

    public override string ToString()
    {
        return $"{Id}: {Severity}";
    }

    public bool Equals(SonarRule? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return RepositoryKey == other.RepositoryKey && Id == other.Id && Severity == other.Severity;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is SonarRule other && Equals(other));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RepositoryKey, Id, (int)Severity);
    }
}
