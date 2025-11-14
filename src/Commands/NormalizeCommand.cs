using System.CommandLine;
using System.Xml.Linq;
using SonarRulesetTool.Accessors;

namespace SonarRulesetTool.Commands;

internal static class NormalizeCommand
{
    public static Command Create()
    {
        var inputFileArgument = new Argument<FileInfo>("inputFile")
        {
            Description = "Path to the .xml file that contains the SonarCloud profile",
            CustomParser = CommandArgumentParser.ParseFileInfo
        };

        var outputFileNameOption = new Option<string?>("--outputFile")
        {
            Description = "Path to the normalized output .xml file (by default, adds -normalized to the file name)"
        };

        var sortByKeyOption = new Option<bool>("--sortByKey")
        {
            Description = "Sort rules alphabetically by key (default: true)",
            DefaultValueFactory = _ => true
        };

        var cleanRuleOption = new Option<bool>("--cleanRule")
        {
            Description = "Only preserve 'key' and 'repositoryKey' per rule (default: true)",
            DefaultValueFactory = _ => true
        };

        var command = new Command("normalize", "Normalizes an exported SonarCloud .xml file for easy diffing")
        {
            inputFileArgument,
            outputFileNameOption,
            sortByKeyOption,
            cleanRuleOption
        };

        command.SetAction(parseResult =>
        {
            FileInfo inputFileInfo = parseResult.GetRequiredValue(inputFileArgument);
            string? outputFileName = parseResult.GetValue(outputFileNameOption);
            bool sortByKey = parseResult.GetValue(sortByKeyOption);
            bool cleanRule = parseResult.GetValue(cleanRuleOption);

            HandleCommand(inputFileInfo, outputFileName, sortByKey, cleanRule);
        });

        return command;
    }

    private static void HandleCommand(FileInfo inputFileInfo, string? outputFileName, bool sortByKey, bool cleanRule)
    {
        string parentDirectory = Path.GetDirectoryName(inputFileInfo.FullName)!;

        string outputPath = Path.Combine(parentDirectory,
            outputFileName ?? $"{Path.GetFileNameWithoutExtension(inputFileInfo.Name)}-normalized{Path.GetExtension(inputFileInfo.Name)}");

        NormalizeFile(inputFileInfo, outputPath, sortByKey, cleanRule);
    }

    private static void NormalizeFile(FileInfo inputFileInfo, string outputPath, bool sortByKey, bool cleanRule)
    {
        XDocument document = SonarCloudAccessor.Read(inputFileInfo.FullName);

        if (cleanRule)
        {
            SonarCloudAccessor.Minimize(document);
        }

        if (sortByKey)
        {
            SonarCloudAccessor.SortByRuleKey(document);
        }

        SonarCloudAccessor.Write(document, outputPath);
    }
}
