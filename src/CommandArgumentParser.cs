using System.CommandLine.Parsing;

namespace SonarRulesetTool;

internal static class CommandArgumentParser
{
    public static FileInfo ParseFileInfo(ArgumentResult result)
    {
        string fileName = result.Tokens.Single().Value;
        var fileInfo = new FileInfo(fileName);

        if (!fileInfo.Exists)
        {
            result.AddError($"File '{fileName}' does not exist.");
        }

        return fileInfo;
    }
}
