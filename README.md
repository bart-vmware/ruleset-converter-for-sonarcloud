# Convert local [SonarAnalyzer](https://www.nuget.org/packages/SonarAnalyzer.CSharp/) rules to [SonarCloud](https://docs.sonarcloud.io/)

The primary goal of this tool is to refresh the Quality Profile in SonarCloud, based on the .ruleset file that runs during local solution build.
At the same time, we don't want to disable rules that exist solely in SonarCloud, so these get merged back in.

# How to use

## Use case: Refresh SonarCloud profile from local RuleSet file

The steps below describe how to download the default profile from SonarCloud and generate an updated XML file that you can upload. The generated file contains the rules that only exist in SonarCloud, combined with the local rule severities.

1. To start, export the 'current' built-in profile from SonarCloud. Because you can't directly export "Sonar way", create a temporary copy.
1. Download the copied profile to disk (organization > Quality Profiles > CopyOfSonarWay > Back Up) and delete the online profile.
1. Update the `SonarAnalyzer.CSharp` PackageReference in this project to exactly the version you use.
1. Run this tool with `convert` and have it generate the new SonarCloud profile XML file.
1. Import the generated file into SonarCloud (organization > Quality Profiles > Restore).

## Use case: Normalize SonarCloud profile XML files for easy diffing

To simplify manual comparisons between profiles, the `normalize` command normalizes an existing XML file by:
- Reformatting and indenting the XML file
- Ordering rules alphabetically
- Removing irrelevant information, such as priority and parameters

## Use case: See what's changed between SonarAnalyzer versions

To see which rules have been added/deleted or changed in severity, the `convert` command with the `--verbose` switch can be used.

1. Update the version of the `SonarAnalyzer.CSharp` PackageReference in this project to the old version.
1. Run `convert` with `--verbose` and copy/paste the output into your diff tool.
1. Update the PackageReference to the new version.
1. Rerun `convert` with `--verbose` and copy/paste the output into your diff tool.

# Notes
- This tool does not preserve custom parameters in SonarCloud rules. There's no point in using them anyway, as they are ignored by SonarAnalyzer.
- The SonarCloud severities are completely unrelated to analyzer severities and therefore quite useless.
- Therefore, customized severities in SonarCloud are not preserved.
