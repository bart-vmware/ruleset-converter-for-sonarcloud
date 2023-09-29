namespace SonarRulesetTool.Models;

internal sealed class SonarRule : IEquatable<SonarRule>
{
    /// <summary>
    /// The "repositoryKey" value used in SonarCloud XML files.
    /// </summary>
    public const string SonarAnalyzerRepositoryKey = "csharpsquid";

    public string RepositoryKey { get; }
    public string Id { get; }
    public RuleSeverity Severity { get; }

    public SonarRule(string id, RuleSeverity severity, string repositoryKey)
    {
        Id = id;
        Severity = severity;
        RepositoryKey = repositoryKey;
    }

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
