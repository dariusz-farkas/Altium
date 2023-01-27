using CommandLine;
using System.Text.RegularExpressions;

namespace Altium.TestTask.ConsoleApp;

[Verb("sort", HelpText = "Sort file using external merge.")]
class SortOption
{
    [Option('f', "file", Required = true, HelpText = "Select file to sort.")]
    public string File { get; set; } = null!;
}

[Verb("create", HelpText = "Create test file.")]
class CreateOption
{
    [Option('f', "file", Required = true, HelpText = "Select file to create or overwrite.")]
    public string File { get; set; } = null!;

    [Option('s', "size", Required = true, HelpText = "Select target size of the file. Number followed by unit [mb, gb, kb].")]
    public Size Size { get; set; }
}

sealed class Size
{
    private static readonly Regex _regex = new Regex("([0-9]+)(\\w{2})");
    public Size(string size)
    {
        var rs = _regex.Match(size);
        if (rs.Success)
        {
            var length = long.Parse(rs.Groups[0].Value);
            ValueInKb = rs.Groups[1].Value switch
            {
                "mb" => length * 1024,
                "kb" => length,
                "gb" => length * 1024 * 1024,
                _ => ValueInKb
            };
        }
        else
        {
            throw new ArgumentException("Could not parse size");
        }

    }

    public long ValueInKb { get; private set; }

}