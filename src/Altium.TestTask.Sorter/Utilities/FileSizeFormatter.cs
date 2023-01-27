namespace Altium.TestTask.Sorter.Utilities;

public static class FileSizeFormatter
{
    private static readonly string[] Suffixes = { "Bytes", "KB", "MB", "GB" };
    public static string FormatSize(long bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentException("bytes cannot be lower than 0.");
        }

        var counter = 0;
        var number = (decimal)bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {Suffixes[counter]}";
    }
}