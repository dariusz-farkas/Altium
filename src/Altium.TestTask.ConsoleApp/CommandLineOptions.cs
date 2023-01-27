using CommandLine;
using System.Text.RegularExpressions;

namespace Altium.TestTask.ConsoleApp;

[Verb("sort", HelpText = "Sort file using external merge.")]
class SortOption
{
    [Option('f', "file", Required = true, HelpText = "Select file to sort.")]
    public string File { get; set; } = null!;
    public void Validate()
    {
        if (!System.IO.File.Exists(File))
        {
            throw new ArgumentException("File does not exists");
        }
    }
}

[Verb("create", HelpText = "Create test file.")]
class CreateOption
{
    [Option('f', "file", Required = true, HelpText = "Select file to create or overwrite.")]
    public string File { get; set; } = null!;

    [Option('s', "size", Required = true, HelpText = "Select target size of the file. Number followed by unit [mb, gb, kb].")]
    public Size Size { get; set; } = null!;

    public void Validate()
    {
        if (Size.ByteLength is < 100L && Size.ByteLength > 128849018880L)
        {
            throw new ArgumentException("File size invalid. Please provide a value between 100 bytes nad 120 gb.");
        }
    }
}

[Verb("verify", HelpText = "Verify test file.")]
class VerifyOption
{
    [Option('f', "file", Required = true, HelpText = "Select file to verify.")]
    public string File { get; set; } = null!;

    public void Validate()
    {
        if (!System.IO.File.Exists(File))
        {
            throw new ArgumentException("File does not exists");
        }
    }
}

sealed class Size
{
    private static readonly Regex _regex = new Regex("([0-9]+)(\\w{2})");
    public Size(string size)
    {
        var rs = _regex.Match(size);
        if (rs.Success)
        {
            var length = long.Parse(rs.Groups[1].Value);
            ByteLength = rs.Groups[2].Value switch
            {
                "mb" => length * 1024 * 1024,
                "kb" => length * 1024,
                "gb" => length * 1024 * 1024 * 1024,
                _ => ByteLength
            };
        }
        else
        {
            throw new ArgumentException("Could not parse size");
        }

    }

    public long ByteLength { get; private set; }

}