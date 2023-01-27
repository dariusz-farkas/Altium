using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Configuration;
using Altium.TestTask.Sorter.Models;
using Altium.TestTask.Sorter.Tests.Core;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;

namespace Altium.TestTask.Sorter.Tests.FilePartitionerTests;

[TestFixture]
public class PartitionTests : UnitTestBase<PartitionTestsContext>
{
    [Test, Combinatorial]
    public async Task WhenPartitioned_ShouldSplitFiles(
        [Values(1, 2, 3, 4, 6, 8, 16, 20, 24, 30, 34, 36, 40)] int bufferSize,
        [Values(2, 8, 10, 12, 18, 20, 40)] int fileSize)
    {
        // arrange
        var testFile = new TestFile("C:\\workspace\\input.txt");

        var inputText = "2, Apple\r\n3. Cos\r\n1. Apple\r\n20. Ala\n";
        // end of line at positions: 10, 18, 28, 36
        var bytes = Encoding.UTF8.GetBytes(inputText);
        this.Context.FileSystem.AddFile(testFile.Path, new MockFileData(bytes));

        this.Context.ConfigureOptions(bufferSize, fileSize);

        // act
        var partitionData = await this.Context.Sut.Partition(testFile, CancellationToken.None);

        //assert
        Assert.That(partitionData.Files.Count, Is.EqualTo(this.Context.Resolutions[fileSize].FileCount));
        var names = partitionData.Files.Select(x => x.FileName).ToList();
        for (var i = 0; i < this.Context.Resolutions[fileSize].FileCount; i++)
        {
            var name = $"unsorted-{i}.data";
            Assert.That(names, Does.Contain(name));

            Assert.That(this.Context.FileSystem.FileExistsInTempDir(name));
            using var stream = this.Context.FileSystem.File.OpenText(this.Context.FileSystem.GetFullPath(name));

            Assert.That(stream.BaseStream.Length, Is.EqualTo(this.Context.Resolutions[fileSize].FileLengths[i]));
            Assert.That(stream.ReadToEnd().ToCharArray().Count(x => x == '\n'), Is.EqualTo(this.Context.Resolutions[fileSize].FileRows[i]));
        }
    }
}

public class PartitionTestsContext : IUnitTestContext
{
    internal Dictionary<int, Solution> Resolutions { get; } = new Dictionary<int, Solution>
    {
        [2] = new Solution(4, new[] { 10, 8, 10, 8 }, new[] { 1, 1, 1, 1 }),
        [8] = new Solution(4, new[] { 10, 8, 10, 8 }, new[] { 1, 1, 1, 1 }),
        [10] = new Solution(3, new[] { 10, 18, 8 }, new[] { 1, 2, 1 }),
        [12] = new Solution(2, new[] { 18, 18 }, new[] { 2, 2 }),
        [18] = new Solution(2, new[] { 18, 18 }, new[] { 2, 2 }),
        [20] = new Solution(2, new[] { 28, 8 }, new[] { 3, 1 }),
        [40] = new Solution(1, new[] { 36 }, new[] { 4 })
    };

    internal FilePartitioner Sut { get; private set; } = null!;
    internal AutoMocker Mock = null!;
    internal TestFileSystem FileSystem { get; private set; } = null!;
    public void Setup()
    {
        Mock = new AutoMocker(MockBehavior.Loose);
        FileSystem = new TestFileSystem("C:\\workspace");
        Mock.Use<IFileSystem>(FileSystem);

        Sut = Mock.CreateInstance<FilePartitioner>();
    }

    public void ConfigureOptions(int bufferSize, int fileSize)
    {
        Mock.GetMock<IOptions<PartitionOptions>>()
            .Setup(x => x.Value)
            .Returns(new PartitionOptions()
            {
                BufferSize = bufferSize,
                FileSize = fileSize
            });
    }



    internal sealed record Solution(int FileCount, int[] FileLengths, int[] FileRows);
}