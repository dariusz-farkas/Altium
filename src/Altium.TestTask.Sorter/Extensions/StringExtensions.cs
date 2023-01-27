namespace Altium.TestTask.Sorter.Extensions;

public static class StringExtensions
{
    public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest)
    {
        first = list.Count > 0 ? list[0] : throw new ArgumentException("Invalid entry.");
        second = list.Count > 1 ? list[1] : throw new ArgumentException("Invalid entry.");
        rest = list.Skip(2).ToList();
    }
}