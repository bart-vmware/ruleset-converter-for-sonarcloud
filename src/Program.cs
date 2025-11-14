using System.CommandLine;
using SonarRulesetTool.Commands;

var rootCommand = new RootCommand("Support tool for SonarAnalyzer and SonarCloud")
{
    ConvertCommand.Create(),
    NormalizeCommand.Create()
};

ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync().ConfigureAwait(false);
