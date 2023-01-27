namespace Altium.TestTask.Sorter.Utilities;

public static class FileSizeFormatter
{
    private static readonly string[] Suffixes = { "Bytes", "KB", "MB", "GB" };
    public static string FormatSize(long bytes)
    {
        int counter = 0;
        decimal number = (decimal)bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number = number / 1024;
            counter++;
        }
        return $"{number:n1}{Suffixes[counter]}";
    }
}