using System.Text;
using Altium.TestTask.Sorter.Abstractions;
using Bogus;

namespace Altium.TestTask.Sorter;

public class FileGenerator
{
    private readonly IFileSystem _fileSystem;
    public FileGenerator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task Generate(long byteSize, string path, CancellationToken cancellationToken)
    {
        await using var stream = _fileSystem.File.Create(path);

        var randomizer = new Faker().Random;

        var duplicated = GetRequiredDuplications(byteSize, randomizer);

        var byteAllocated = 0L;
        while (byteAllocated < byteSize)
        {
            // decide whether take duplicated string or generate fresh new.
            var intPart = randomizer.UInt(0, 100000);

            string stringPart;
            if (intPart % 10 == 1)
            {
                var index = randomizer.Int(0, duplicated.Count);
                (stringPart, _) = duplicated.ElementAt(index);

                duplicated[stringPart]--;
                if (duplicated[stringPart] <= 0)
                {
                    duplicated.Remove(stringPart);
                }
            }
            else
            {
                stringPart = string.Join(' ',
                    Enumerable.Range(0, randomizer.Byte(1, 5)).Select(_ => randomizer.String(4, 10)));
            }

            var line = $"{intPart}. {stringPart}\r\n";

            var lineBytes = Encoding.UTF8.GetBytes(line);
            byteAllocated += lineBytes.Length;
            await stream.WriteAsync(lineBytes, 0, lineBytes.Length, cancellationToken);
        }

    }

    private static Dictionary<string, int> GetRequiredDuplications(long byteSize, Randomizer randomizer)
    {
        var maxDuplicatedWordsSize = randomizer.UShort(10, (ushort)Math.Min(ushort.MaxValue, (byteSize + 1000) / 100));

        var duplicated = new Dictionary<string, int>();
        var dupIndex = 0;
        while (dupIndex < maxDuplicatedWordsSize)
        {
            var wordCount = randomizer.Byte(1, 5);
            string stringPart;
            do
            {
                stringPart = string.Join(' ', Enumerable.Range(0, wordCount).Select(_ => randomizer.String(4, 10)));
            } while (duplicated.ContainsKey(stringPart));

            var dupCount = randomizer.Byte(1, 5);
            duplicated.Add(stringPart, dupCount);

            var totalSize = Encoding.UTF8.GetByteCount(stringPart) * dupCount;
            dupIndex += totalSize;
        }

        return duplicated;
    }
}