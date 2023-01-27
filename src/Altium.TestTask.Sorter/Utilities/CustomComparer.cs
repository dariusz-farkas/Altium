using Altium.TestTask.Sorter.Extensions;

namespace Altium.TestTask.Sorter.Utilities;

public sealed class CustomComparer : IComparer<string>
{
    public static CustomComparer Default { get; } = new CustomComparer();

    public int Compare(string? x, string? y)
    {
        var (x1, x2, _) = x!.Split('.', 2);
        var (y1, y2, _) = y!.Split('.', 2);

        return (string.Compare(x2, y2, StringComparison.Ordinal), int.Parse(x1).CompareTo(int.Parse(y1))) switch
        {
            ( < 0, _) => -1,
            ( > 0, _) => 1,
            (_, -1) => -1,
            (_, 1) => 1,
            (_, _) => 0
        };
    }
}
public sealed class SetCustomComparer : IComparer<(string, string)>
{
    public static CustomComparer Default { get; } = new CustomComparer();

    public int Compare((string, string) x, (string, string) y)
    {
        var (x1, x2, _) = x.Item1.Split('.', 2);
        var (y1, y2, _) = y.Item1!.Split('.', 2);

        return (string.Compare(x2, y2, StringComparison.Ordinal), int.Parse(x1).CompareTo(int.Parse(y1))) switch
        {
            ( < 0, _) => -1,
            ( > 0, _) => 1,
            (_, -1) => -1,
            (_, 1) => 1,
            (_, _) => 0
        };
    }

    public int Compare() => throw new NotImplementedException();
}