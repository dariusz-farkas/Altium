using System.Text;
using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Configuration;
using Bogus;
using Microsoft.Extensions.Options;

namespace Altium.TestTask.Sorter;

public class FileGenerator
{
    private readonly IFileSystem _fileSystem;
    private readonly IOptions<SortOptions> _options;

    public FileGenerator(IFileSystem fileSystem, IOptions<SortOptions> options)
    {
        _fileSystem = fileSystem;
        _options = options;
    }

    public async Task<long> Generate(long byteSize, string path, CancellationToken cancellationToken)
    {
        await using var stream = _fileSystem.File.Create(path, bufferSize: _options.Value.OutputBufferSize);

        var randomizer = new Faker().Random;

        var duplicated = GetRequiredDuplications(byteSize, randomizer);

        bool ShouldUseDuplicatedWord(uint intPart) => intPart % 30 == 1 && duplicated.Any();

        var byteAllocated = 0L;
        var maxIntPart = (uint) byteSize / 1000;
        while (byteAllocated < byteSize)
        {
            // decide whether take duplicated string or generate fresh new.
            
            var intPart = randomizer.UInt(0, maxIntPart);

            var stringPart = ShouldUseDuplicatedWord(intPart)
                ? GetLineFromDuplicatedDict(randomizer, duplicated)
                : GenerateStringLine(randomizer);

            var line = $"{intPart}. {stringPart}\r\n";

            var lineBytes = Encoding.UTF8.GetBytes(line);
            byteAllocated += lineBytes.Length;
            await stream.WriteAsync(lineBytes, 0, lineBytes.Length, cancellationToken);
        }

        return byteAllocated;
    }

    private static string GetLineFromDuplicatedDict(Randomizer randomizer, Dictionary<string, int> duplicated)
    {
        var index = randomizer.Int(0, duplicated.Count - 1);
        var (stringPart, _) = duplicated.ElementAt(index);

        duplicated[stringPart]--;
        if (duplicated[stringPart] <= 0)
        {
            duplicated.Remove(stringPart);
        }

        return stringPart;
    }

    private static Dictionary<string, int> GetRequiredDuplications(long byteSize, Randomizer randomizer)
    {
        var maxDuplicatedWordsSize = randomizer.UShort(10, (ushort)Math.Min(ushort.MaxValue, (byteSize + 1000) / 100));

        var duplicated = new Dictionary<string, int>();
        var dupIndex = 0;
        while (dupIndex < maxDuplicatedWordsSize)
        {
            string stringPart;
            do
            {
                stringPart = GenerateStringLine(randomizer);
            } while (duplicated.ContainsKey(stringPart));

            var dupCount = randomizer.Byte(1, 5);
            duplicated.Add(stringPart, dupCount);

            var totalSize = Encoding.UTF8.GetByteCount(stringPart) * dupCount;
            dupIndex += totalSize;
        }

        return duplicated;
    }

    private static string GenerateStringLine(Randomizer randomizer) =>
        string.Join(' ',
            Enumerable.Range(0, randomizer.Byte(1, 5)).Select(_ => randomizer.Word()/*String2(4, 10)*/));

}