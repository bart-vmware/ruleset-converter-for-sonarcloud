using System.CommandLine;
using System.Xml.Linq;
using SonarRulesetTool.Accessors;

namespace SonarRulesetTool.Commands;

internal static class NormalizeCommand
{
    public static Command Register()
    {
        var inputFileArgument = new Argument<FileInfo>("inputFile", CommandArgumentParser.ParseFileInfo, false,
            "Path to the .xml file that contains the SonarCloud profile");

        var outputFileNameOption = new Option<string?>("--outputFile", "Path to the normalized output .xml file");
        var sortByKeyOption = new Option<bool>("--sortByKey", () => true, "Sort rules alphabetically by key");
        var cleanRuleOption = new Option<bool>("--cleanRule", () => true, "Only preserve 'key' and 'repositoryKey' per rule");

        var command = new Command("normalize", "Normalizes an exported SonarCloud .xml file for easy diffing")
        {
            inputFileArgument,
            outputFileNameOption,
            sortByKeyOption,
            cleanRuleOption
        };

        command.SetHandler(HandleCommand, inputFileArgument, outputFileNameOption, sortByKeyOption, cleanRuleOption);

        return command;
    }

    private static void HandleCommand(FileInfo inputFileInfo, string? outputFileName, bool sortByKey, bool cleanRule)
    {
        string parentDirectory = Path.GetDirectoryName(inputFileInfo.FullName)!;
        string outputPath = Path.Combine(parentDirectory, outputFileName ?? Path.GetFileNameWithoutExtension(inputFileInfo.Name) + "-normalized.xml");

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
