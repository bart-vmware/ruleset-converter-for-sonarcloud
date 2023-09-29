using System.CommandLine;
using SonarRulesetTool.Commands;

var rootCommand = new RootCommand("Support tool for SonarAnalyzer and SonarCloud")
{
    ConvertCommand.Register(),
    NormalizeCommand.Register()
};

return await rootCommand.InvokeAsync(args);
